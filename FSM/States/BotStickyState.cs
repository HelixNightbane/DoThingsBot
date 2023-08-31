using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using DoThingsBot.Lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DoThingsBot.FSM.States {
    class BotStickyState : IBotState {
        public string Name { get => "BotStickyState"; }
        private DateTime lastThought = DateTime.MinValue;
        private DateTime headingChangeFinishTime = DateTime.MinValue;
        private Machine _machine;
        private bool hasWandEquipped;
        private bool needsKeyReset;
        private bool holdingShift;
        private bool holdingForward;
        private float lastDistance;

        public BotStickyState() {

        }

        public void Enter(Machine machine) {
            _machine = machine;
            hasWandEquipped = Util.HasWandEquipped();
        }

        public void Exit(Machine machine) {
            StopMovement();
        }

        private static double QuaternionToHeading(Vector4Object q) {
            return Math.Atan2(2 * (q.W * q.Z + q.X * q.Y), 1 - 2 * (q.Y * q.Y + q.Z * q.Z));
        }

        public void Think(Machine machine) {
            if (CoreManager.Current.Actions.ChatState) {
                Util.WriteToChat("Chat window is open and has focus, unable to move to sticky position!");
                machine.ChangeState(new BotIdleState());
                return;
            }

            if (!Config.Bot.EnableStickySpot.Value) {
                machine.ChangeState(new BotIdleState());
                return;
            }

            if (!CheckHeading()) {
                needsKeyReset = true;
                return;
            }

            if (!CheckDistance())
                return;

            machine.ChangeState(new BotIdleState());
        }

        [DllImport("user32.dll")]
        static extern bool SetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetKeyboardState(byte[] lpKeyState);

        const int VK_SHIFT = 0x10;

        public static unsafe Vector3Object GetPosition(int id) {
            if (CoreManager.Current.Actions.IsValidObject(id)) {
                var p = CoreManager.Current.Actions.Underlying.GetPhysicsObjectPtr(id);
                return new Vector3Object(*(float*)(p + 0x84), *(float*)(p + 0x88), *(float*)(p + 0x8C));
            }

            return new Vector3Object(0,0,0);
        }

        internal static unsafe Vector4Object GetRot(int id) {
            if (CoreManager.Current.Actions.IsValidObject(id)) {
                var p = CoreManager.Current.Actions.Underlying.GetPhysicsObjectPtr(id);
                return new Vector4Object(*(float*)(p + 0x50), *(float*)(p + 0x54), *(float*)(p + 0x58), *(float*)(p + 0x5C));
            }

            return new Vector4Object(0, 0, 0, 0);
        }

        public static unsafe uint GetLandcell(int id) {
            if (CoreManager.Current.Actions.IsValidObject(id)) {
                var p = CoreManager.Current.Actions.Underlying.GetPhysicsObjectPtr(id);
                return (uint)*(int*)(p + 0x4C);
            }

            return 0;
        }

        public static int LandblockXDifference(uint originLandblock, uint landblock) {
            var olbx = originLandblock >> 24;
            var lbx = landblock >> 24;

            return (int)(lbx - olbx) * 192;
        }

        public static int LandblockYDifference(uint originLandblock, uint landblock) {
            var olby = originLandblock << 8 >> 24;
            var lby = landblock << 8 >> 24;

            return (int)(lby - olby) * 192;
        }

        public static uint GetLandblockFromCoordinates(double EW, double NS) {
            NS -= 0.5f;
            EW -= 0.5f;
            NS *= 10.0f;
            EW *= 10.0f;

            uint basex = (uint)(EW + 0x400);
            uint basey = (uint)(NS + 0x400);

            if ((int)(basex) < 0 || (int)(basey) < 0 || basex >= 0x7F8 || basey >= 0x7F8) {
                Console.WriteLine("Out of Bounds");
            }
            byte blockx = (byte)(basex >> 3);
            byte blocky = (byte)(basey >> 3);
            byte cellx = (byte)(basex & 7);
            byte celly = (byte)(basey & 7);

            int block = (blockx << 8) | (blocky);
            int cell = (cellx << 3) | (celly);

            int dwCell = (block << 16) | (cell + 1);

            return (uint)dwCell;
        }

        public static float NSToLandblock(uint landcell, float ns) {
            uint l = (uint)((landcell & 0x00FF0000) / 0x2000);
            var yOffset = ((ns * 10) - l + 1019.5) * 24;
            return (float)yOffset;
        }

        public static float EWToLandblock(uint landcell, float ew) {
            uint l = (uint)((landcell & 0xFF000000) / 0x200000);
            var yOffset = ((ew * 10) - l + 1019.5) * 24;
            return (float)yOffset;
        }

        public static PointF LandblockOffsetFromCoordinates(float ew, float ns) {
            var landblock = GetLandblockFromCoordinates(ew, ns);
            return new PointF(
                    EWToLandblock(landblock, ew),
                    NSToLandblock(landblock, ns)
            );
        }

        private bool CheckDistance() {
            var distance = GetDistanceToStickySpot();

            if (lastDistance == distance)
                needsKeyReset = true;

            lastDistance = distance;

            if (distance > Config.Bot.StickySpotMaxDistance.Value) {
                if (distance > 100) { // dont nav more than 100 meters
                    StopMovement();
                    _machine.ChangeState(new BotIdleState());
                    return false;
                }
                ChangeMovement(true, distance < 3);
                return false;
            }

            ChangeMovement(false, false);
            return true;
        }

        public static float GetDistanceToStickySpot() {
            // todo: this whole thing sux, why am i so bad at math
            // iso: someone to rewrite this whole math stuff
            var currentLB = GetLandcell(CoreManager.Current.CharacterFilter.Id);
            var targetLB = GetLandblockFromCoordinates(Config.Bot.StickySpotEW.Value, Config.Bot.StickySpotNS.Value);
            var targetOffset = LandblockOffsetFromCoordinates((float)Config.Bot.StickySpotEW.Value, (float)Config.Bot.StickySpotNS.Value);
            var currentOffset = GetPosition(CoreManager.Current.CharacterFilter.Id);

            // normalize offsets/landblocks
            while (currentOffset.Y < 0) {
                currentOffset = new Vector3Object(currentOffset.X, currentOffset.Y + 192, 0);
                currentLB += 0x00010000;
            }
            while (currentOffset.X < 0) {
                currentOffset = new Vector3Object(currentOffset.X + 192, currentOffset.Y, 0);
                currentLB += 0x01000000;
            }

            var xDiff = (LandblockXDifference(currentLB, targetLB) * 192) + (targetOffset.X - currentOffset.X);
            var yDiff = (LandblockXDifference(currentLB, targetLB) * 192) + (targetOffset.Y - currentOffset.Y);

            //Util.WriteToChat($"CurrentLB: {currentLB:X8} targetLB: {targetLB:X8} targetOffset:{targetOffset.X},{targetOffset.Y} currentOffset:{currentOffset.X},{currentOffset.Y} xDiff:{xDiff} yDiff:{yDiff}");
            return (float)Math.Sqrt(Math.Pow(xDiff, 2) + Math.Pow(yDiff, 2));
        }

        private void ChangeMovement(bool shouldMove, bool shouldWalk) {
            if (CoreManager.Current.Actions.ChatState)
                return;

            if (needsKeyReset) {
                StopMovement();
            }

            if (shouldMove) {
                if (shouldWalk) {
                    User32.PostMessage(CoreManager.Current.Decal.Hwnd, User32.WM_KEYDOWN, (IntPtr)VK_SHIFT, (UIntPtr)0x002A0001);
                    holdingShift = true;
                }
                User32.PostMessage(CoreManager.Current.Decal.Hwnd, User32.WM_KEYDOWN, (IntPtr)Globals.Host.GetKeyboardMapping("MovementForward"), (UIntPtr)0x00110001);
                holdingForward = true;
            }
            else {
                StopMovement();
            }
        }

        private void StopMovement() {
            if (CoreManager.Current.Actions.ChatState)
                return;

            if (holdingForward)
                User32.PostMessage(CoreManager.Current.Decal.Hwnd, User32.WM_KEYUP, (IntPtr)Globals.Host.GetKeyboardMapping("MovementForward"), (UIntPtr)0xC0110001);
            if (holdingShift)
                User32.PostMessage(CoreManager.Current.Decal.Hwnd, User32.WM_KEYUP, (IntPtr)VK_SHIFT, (UIntPtr)0xC02A0001);
            User32.PostMessage(CoreManager.Current.Decal.Hwnd, User32.WM_KEYDOWN, (IntPtr)Globals.Host.GetKeyboardMapping("MovementStop"), (UIntPtr)0x00110001);
            User32.PostMessage(CoreManager.Current.Decal.Hwnd, User32.WM_KEYUP, (IntPtr)Globals.Host.GetKeyboardMapping("MovementStop"), (UIntPtr)0xC0110001);
            needsKeyReset = false;
            holdingShift = false;
            holdingForward = false;
        }

        private bool CheckHeading() {
            var co = new CoordsObject(Config.Bot.StickySpotNS.Value, Config.Bot.StickySpotEW.Value);
            var currentCoords = CoreManager.Current.WorldFilter[CoreManager.Current.CharacterFilter.Id].Coordinates();
            var wantedAngle = currentCoords.AngleToCoords(co);
            var currentAngle = CoreManager.Current.Actions.Heading;
            var diff = Math.Abs((wantedAngle + 1000) - (currentAngle + 1000));
            var physicsAngle = 360 - (QuaternionToHeading(GetRot(CoreManager.Current.CharacterFilter.Id)) * 180/Math.PI);
            //return false;

            if (diff < 1)
                return true;

            if (DateTime.UtcNow < headingChangeFinishTime)
                return false;

            headingChangeFinishTime = DateTime.UtcNow + TimeSpan.FromMilliseconds(diff * 15);
            Globals.Core.Actions.FaceHeading(currentCoords.AngleToCoords(co), true);
            return false;
        }

        public ItemBundle GetItemBundle() {
            return null;
        }
    }
}
