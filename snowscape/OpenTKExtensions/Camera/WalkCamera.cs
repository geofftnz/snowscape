using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Input;
using OpenTK;

namespace OpenTKExtensions.Camera
{
    public class WalkCamera : ICamera
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


        /// <summary>
        /// look angle (up/down), in radians 
        /// </summary>
        private float angleUpDown = (float)Math.PI * 0.5f;
        private float MINANGLEUPDOWN = (float)0.01f;
        private float MAXANGLEUPDOWN = (float)Math.PI * 0.99f;

        private int prevMouseX = -10000;
        private int prevMouseY = -10000;
        float movementSpeed = 5f;


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


        public WalkCamera()
        {
            this.Projection = Matrix4.Identity;

            this.Position = new Vector3(0.5f, 0.5f, 0f);
            this.AngleUpDown = (float)Math.PI * 0.5f;
            this.AngleLeftRight = (float)Math.PI;
            this.EyeHeight = 200f;
            this.ZNear = 1.0f;
            this.ZFar = 4000.0f;

            this.ViewEnable = true;

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



        public void Update(double time)
        {

            // mouse look
            if (ViewEnable)
            {
                int mouseX = Mouse.X, mouseY = Mouse.Y;

                if (prevMouseX <= -10000) prevMouseX = mouseX;
                if (prevMouseY <= -10000) prevMouseY = mouseY;

                //int deltaX = (this.Width / 2) - mouseX;
                //int deltaY = (this.Height / 2) - mouseY;
                int deltaX = mouseX - prevMouseX;
                int deltaY = mouseY - prevMouseY;

                this.AngleLeftRight += (float)deltaX * -0.01f;
                this.AngleUpDown += (float)deltaY * -0.01f;

                prevMouseX = mouseX;
                prevMouseY = mouseY;
            }

            // keyboard move
            float speed = (float)(this.movementSpeed * time * Math.Sqrt(this.EyeHeight));

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
                this.EyeHeight += 10.0f * this.movementSpeed * (float)time;
                this.IsMoving = true;
            }
            if (this.Keyboard[Key.V])
            {
                this.EyeHeight -= 10.0f * this.movementSpeed * (float)time;
                this.IsMoving = true;
            }

            this.Position = pos;

        }

        public Matrix4 Projection
        {
            get;
            private set;
        }

        public Matrix4 View
        {
            get { return Matrix4.LookAt(this.EyePos, this.LookTarget, -Vector3.UnitY); }
        }
    }
}
