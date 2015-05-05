using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Input;
using OpenTK;
using OpenTKExtensions.Framework;

namespace OpenTKExtensions.Camera
{
    public class WalkCamera : GameComponentBase, ICamera, IUpdateable, IResizeable
    {

        public KeyboardDevice Keyboard { get; set; }
        public MouseDevice Mouse { get; set; }
        public Func<Vector3, Vector3> PositionClampFunc { get; set; }

        public Vector3 Position { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public float ZNear { get; set; }
        public float ZFar { get; set; }

        public bool ViewEnable { get; set; }

        public Vector3 Eye { get { return EyePos; } }

        public enum LookModeEnum
        {
            Always=0,
            Mouse1,
            Mouse2
        }
        public LookModeEnum LookMode { get; set; }


        /// <summary>
        /// look angle (up/down), in radians 
        /// </summary>
        private float angleUpDown = (float)Math.PI * 0.5f;
        private float MINANGLEUPDOWN = (float)0.01f;
        private float MAXANGLEUPDOWN = (float)Math.PI * 0.99f;

        private int prevMouseX = -10000;
        private int prevMouseY = -10000;

        public float MovementSpeed { get; set; }

        private Random rand = new Random();


        public float AngleUpDown
        {
            get { return angleUpDown; }
            set
            {

                if (value < MINANGLEUPDOWN)
                {
                    angleUpDown = MINANGLEUPDOWN;
                }
                else
                    if (value > MAXANGLEUPDOWN)
                    {
                        angleUpDown = MAXANGLEUPDOWN;
                    }
                    else
                    {
                        angleUpDown = value;
                    }

            }
        }

        /// <summary>
        /// look angle (left/right), in radians
        /// </summary>
        private float angleLeftRight = 0.0f;
        public float AngleLeftRight
        {
            get { return angleLeftRight; }
            set
            {
                angleLeftRight = value;
            }
        }

        /// <summary>
        /// height of eye above ground
        /// </summary>
        public float EyeHeight { get; set; }

        /// <summary>
        /// true when player is moving
        /// </summary>
        public bool IsMoving { get; set; }

        public bool IsMouseMoving { get; set; }

        public Vector3 EyePos
        {
            get
            {
                return this.Position + Vector3.UnitY * this.EyeHeight;
            }
        }

        public Vector3 LookTarget
        {
            get
            {
                return this.EyePos + new Vector3((float)(Math.Cos(this.AngleLeftRight) * Math.Sin(AngleUpDown)), (float)Math.Cos(AngleUpDown), (float)(Math.Sin(this.AngleLeftRight) * Math.Sin(AngleUpDown)));
            }
        }

        public float DitherAmount { get; set; }
        public Vector3 LookTargetDithered
        {
            get
            {
                float ditheramount = DitherAmount;
                float lrdither = (float)((rand.NextDouble() - 0.5)) * ditheramount;
                float uddither = (float)((rand.NextDouble() - 0.5)) * ditheramount;

                return this.EyePos + new Vector3((float)(Math.Cos(this.AngleLeftRight + lrdither) * Math.Sin(AngleUpDown + uddither)), (float)Math.Cos(AngleUpDown + uddither), (float)(Math.Sin(this.AngleLeftRight + lrdither) * Math.Sin(AngleUpDown + uddither)));
            }
        }

        public WalkCamera()
        {
            this.Projection = Matrix4.Identity;

            this.Position = new Vector3(0.5f, 0.5f, 0f);
            this.AngleUpDown = (float)Math.PI * 0.5f;
            this.AngleLeftRight = (float)Math.PI;
            this.EyeHeight = 1f;
            this.ZNear = 1.0f;
            this.ZFar = 4000.0f;
            this.MovementSpeed = 5.0f;

            this.ViewEnable = true;

            this.DitherAmount = 0.0005f;

            this.LookMode = LookModeEnum.Always;

        }

        public WalkCamera(KeyboardDevice k, MouseDevice m)
            : this()
        {
            this.Keyboard = k;
            this.Mouse = m;
        }

        public void Resize(int ClientWidth, int ClientHeight)
        {
            this.Width = ClientWidth;
            this.Height = ClientHeight;

            this.Projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI * 0.4f, ClientHeight > 0 ? (float)ClientWidth / (float)ClientHeight : 1.0f, this.ZNear, this.ZFar);
        }

        private bool previousViewMouseButton = false;

        public void Update(IFrameUpdateData frameData)
        {
            double time = frameData.Time;
            if (time < 0.0001) time = 0.0001;

            // mouse look
            if 
                (
                    ViewEnable &&
                    (
                        (LookMode == LookModeEnum.Always) ||
                        (LookMode == LookModeEnum.Mouse1 && Mouse[MouseButton.Left]) ||
                        (LookMode == LookModeEnum.Mouse2 && Mouse[MouseButton.Right])
                    )
                )
            {
                int mouseX = Mouse.X, mouseY = Mouse.Y;

                if (prevMouseX <= -10000 || !previousViewMouseButton) prevMouseX = mouseX;
                if (prevMouseY <= -10000 || !previousViewMouseButton) prevMouseY = mouseY;

                //int deltaX = (this.Width / 2) - mouseX;
                //int deltaY = (this.Height / 2) - mouseY;
                int deltaX = mouseX - prevMouseX;
                int deltaY = mouseY - prevMouseY;

                IsMouseMoving = (IsMouseMoving || deltaX != 0 || deltaY != 0);

                this.AngleLeftRight += (float)deltaX * -0.01f;
                this.AngleUpDown += (float)deltaY * -0.01f;

                prevMouseX = mouseX;
                prevMouseY = mouseY;
            }
            if (LookMode == LookModeEnum.Mouse1) previousViewMouseButton = Mouse[MouseButton.Left];
            if (LookMode == LookModeEnum.Mouse2) previousViewMouseButton = Mouse[MouseButton.Right];

            // keyboard move
            float speed = (float)(this.MovementSpeed * time);

            float speedmul = this.Keyboard[Key.ShiftLeft] ? 1.0f : 0.1f;
            speed *= speedmul;

            var pos = this.Position;
            if (this.Keyboard[Key.W])
            {
                pos.X += (float)(Math.Cos(this.AngleLeftRight) * speed);
                pos.Z += (float)(Math.Sin(this.AngleLeftRight) * speed);
                this.IsMoving = true;
            }
            if (this.Keyboard[Key.S])
            {
                pos.X -= (float)(Math.Cos(this.AngleLeftRight) * speed);
                pos.Z -= (float)(Math.Sin(this.AngleLeftRight) * speed);
                this.IsMoving = true;
            }
            if (this.Keyboard[Key.A])
            {
                pos.X += (float)(Math.Cos(this.AngleLeftRight + Math.PI * 0.5) * speed);
                pos.Z += (float)(Math.Sin(this.AngleLeftRight + Math.PI * 0.5) * speed);
                this.IsMoving = true;
            }
            if (this.Keyboard[Key.D])
            {
                pos.X += (float)(Math.Cos(this.AngleLeftRight + Math.PI * 1.5) * speed);
                pos.Z += (float)(Math.Sin(this.AngleLeftRight + Math.PI * 1.5) * speed);
                this.IsMoving = true;
            }

            if (this.Keyboard[Key.F])
            {
                this.EyeHeight += this.MovementSpeed * speedmul * (float)time;
                this.IsMoving = true;
            }
            if (this.Keyboard[Key.V])
            {
                this.EyeHeight -= this.MovementSpeed * speedmul * (float)time;
                this.IsMoving = true;
            }

            this.Position = pos;

        }

        public bool HasChanged()
        {
            return IsMoving || IsMouseMoving;
        }

        public void ResetChanged()
        {
            IsMoving = false;
            IsMouseMoving = false;
        }

        public Matrix4 Projection
        {
            get;
            private set;
        }

        public Matrix4 View
        {
            get
            {
                //Vector3 lookDither = Vector3.Zero;
                //float ditherAmount = 0.0002f;

                //lookDither.X = (float)((rand.NextDouble() - 0.5));
                //lookDither.Y = (float)((rand.NextDouble() - 0.5));
                //lookDither.Z = (float)((rand.NextDouble() - 0.5));
                //return Matrix4.LookAt(this.EyePos, this.LookTarget + lookDither * ditherAmount, -Vector3.UnitY);

                return Matrix4.LookAt(this.EyePos, this.HasChanged() ? this.LookTarget : this.LookTargetDithered, -Vector3.UnitY);
            }
        }

    }
}
