using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using OpenTK;
using OpenTK.Input;

namespace OpenTKExtensions.Camera
{
    // credit: opcon, from http://www.opentk.com/node/3016
    public class QuaternionCamera : ICamera
    {
        #region Constructors

        /// <summary>
        /// Creates a new Quaternion Camera
        /// </summary>
        /// <param name="moused">
        /// A <see cref="MouseDevice"/> reference from the current game
        /// </param>
        /// <param name="keyd">
        /// A <see cref="KeyboardDevice"/> reference from the current game
        /// </param>
        /// <param name="bounds">
        /// A <see cref="Rectangle"/> 
        /// </param>
        /// <param name="client">
        /// A <see cref="Rectangle"/>
        /// </param>
        /// <param name="mouseLook">
        /// A <see cref="System.Boolean"/> indicating wether mouse look is enabled
        /// </param>
        /// <param name="game">
        /// A <see cref="INativeWindow"/> reference to the current game window
        /// </param>
        //public QuaternionCamera(MouseDevice moused, KeyboardDevice keyd, bool mouseLook, INativeWindow game)
        //    : this(moused, keyd, game, new Vector3(), new Quaternion() { X = 0, Y = 0, Z = 1, W = 0 }, mouseLook)
        //{ }

        //public QuaternionCamera(Vector3 position, MouseDevice moused,
        //                        KeyboardDevice keyd, bool m, INativeWindow game)
        //    : this(moused, keyd, game, position, new Quaternion() { X = 0, Y = 0, Z = 0, W = 0 }, m)
        //{ }

        public QuaternionCamera(MouseDevice moused, KeyboardDevice keyd, INativeWindow game, Vector3 position = new Vector3(), Quaternion orientation = new Quaternion(), bool mouseLook = true)
        {
            Speed = 0.2f;
            TargetPosition = Position = position;
            TargetOrientation = Orientation = orientation;
            m_Mouse = moused;
            m_Keyboard = keyd;
            m_ParentGame = game;
            MouseRotation = new Vector2(0, 0);
            Movement = new Vector3(0, 0, 0);
            MouseLookEnabled = mouseLook;

            AspectRatio = 1.333f;
            FieldOfView = 60;
            ZNear = 0.1f;
            ZFar = 64;

            if (MouseLookEnabled)
            {
                //Cursor.Hide();
                ResetMouse();
                ApplyRotation();
                Orientation = TargetOrientation;
            }
        }

        #endregion

        #region Members

        #region Properties

        public Vector3 Position { get; set; }
        public Quaternion Orientation { get; set; }

        public Vector3 TargetPosition { get; set; }
        public Quaternion TargetOrientation { get; set; }
        public Quaternion TargetOrientationY { get; set; }

        protected bool m_MouseLookEnabled;
        public bool MouseLookEnabled
        {
            get { return m_MouseLookEnabled; }
            set
            {
                m_MouseLookEnabled = value;
                if (MouseLookEnabled)
                {
                    Cursor.Hide(); ResetMouse();
                }
                else
                {
                    Cursor.Show();
                }
            }
        }

        public float MouseYSensitivity { get; set; }
        public float MouseXSensitivity { get; set; }

        public Vector2 MouseRotation;
        public Vector3 Movement;

        public float Speed { get; set; }
        public float Acceleration { get; set; }

        protected Point WindowCenter
        { get { return new Point((m_ParentGame.ClientRectangle.Left + m_ParentGame.ClientRectangle.Right) / 2, (m_ParentGame.ClientRectangle.Top + m_ParentGame.ClientRectangle.Bottom) / 2); } }

        public float ZNear { get; set; }
        public float ZFar { get; set; }
        public float FieldOfView { get; set; }
        public float AspectRatio { get; set; }

        public CamMode CameraMode { get; protected set; }

        #endregion

        #region Protected Variables

        /// <summary>
        /// The current Mouse Delta (How much the mouse has moved since the last frame
        /// </summary>
        protected Point m_MouseDelta;

        protected MouseDevice m_Mouse;
        protected KeyboardDevice m_Keyboard;

        protected INativeWindow m_ParentGame;


        #endregion

        #region Public Methods

        public void Update(double time)
        {
            if (time == 0)
                return;

            if (MouseLookEnabled)
            {
                UpdateRotations(time);
            }
            UpdateMovement(time);


            if (TargetOrientation != Orientation)
            {
                Orientation = Quaternion.Slerp(Orientation, TargetOrientation, (float)time);
            }

            if (TargetPosition != Position)
            {
                Position = Vector3.Lerp(Position, TargetPosition, (float)time);
            }

        }

        public void GetProjectionMatrix(out Matrix4 matrix)
        {
            matrix = Matrix4.CreatePerspectiveFieldOfView((float)(FieldOfView * Math.PI / 180.0), AspectRatio, ZNear, ZFar);
        }

        public void GetModelviewMatrix(out Matrix4 matrix)
        {
            var translationMatrix = Matrix4.CreateTranslation(-Position);
            var rotationMatrix = Matrix4.CreateFromQuaternion(Orientation);
            Matrix4.Mult(ref translationMatrix, ref rotationMatrix, out matrix);
        }

        public void GetModelviewProjectionMatrix(out Matrix4 result)
        {
            Matrix4 modelview;
            GetProjectionMatrix(out result);
            GetModelviewMatrix(out modelview);
            Matrix4.Mult(ref modelview, ref result, out result);
        }

        /// <summary>
        /// Updates the Parent game object used for window calculations
        /// </summary>
        /// <param name="game">
        /// A <see cref="INativeWindow"/>
        /// </param>
        public void UpdateParentGame(INativeWindow game)
        {
            m_ParentGame = game;
        }

        /// <summary>
        /// Sets up this camera with the specified Camera Mode
        /// </summary>
        /// <param name="mode">
        /// A <see cref="CamMode"/>
        /// </param>
        public void SetCameraMode(CamMode mode)
        {
            CameraMode = mode;
        }

        public void FixMouse()
        {
            ResetMouse();
            CalculateMouseDelta();
            ApplyRotation();
            Orientation = TargetOrientation;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Calculates the Mouse Delta (How far it has moved from the last update) from the center of the screen
        /// </summary>
        protected void CalculateMouseDelta()
        {
            Point m_MouseCurrent;
            if (m_Mouse.X == 0 || m_Mouse.Y == 0)
                m_MouseCurrent = WindowCenter;
            else
                m_MouseCurrent = new Point(m_Mouse.X, m_Mouse.Y);

            m_MouseDelta = new Point(
                m_MouseCurrent.X - WindowCenter.X,
                m_MouseCurrent.Y - WindowCenter.Y);
        }

        /// <summary>
        /// Resets the mouse to the center of the screen
        /// </summary>
        protected void ResetMouse()
        {
            
            if (m_ParentGame.WindowState == WindowState.Fullscreen)
            {
                Cursor.Position = WindowCenter;
            }

            else
            {
                Cursor.Position = m_ParentGame.PointToScreen(WindowCenter);
            }

        }

        /// <summary>
        /// Clamps the mouse rotation values
        /// </summary>
        protected void ClampMouseValues()
        {
            if (MouseRotation.Y >= 1.57) //90 degrees in radians
                MouseRotation.Y = 1.57f;
            if (MouseRotation.Y <= -1.57)
                MouseRotation.Y = -1.57f;

            if (MouseRotation.X >= 6.28) //360 degrees in radians (or something in radians)
                MouseRotation.X -= 6.28f;
            if (MouseRotation.X <= -6.28)
                MouseRotation.X += 6.28f;
        }

        /// <summary>
        /// Updates the Orientation Quaternion for this camera using the calculated Mouse Delta
        /// </summary>
        /// <param name="time">
        /// A <see cref="System.Double"/> containing the time since the last update
        /// </param>
        protected void UpdateRotations(double time)
        {
            CalculateMouseDelta();
            MouseRotation.X += (float)(m_MouseDelta.X * MouseXSensitivity * time);
            MouseRotation.Y += (float)(m_MouseDelta.Y * MouseYSensitivity * time);

            ClampMouseValues();
            ApplyRotation();
            if (CameraMode != CamMode.FlightCamera)
            {
                Orientation = TargetOrientation;
            }
            ResetMouse();
        }

        protected void ApplyRotation()
        {
            TargetOrientationY = Quaternion.FromAxisAngle(Vector3.UnitY, (MathHelper.Pi + MouseRotation.X));
            TargetOrientation = Quaternion.FromAxisAngle(Vector3.UnitX, MouseRotation.Y) *
            TargetOrientationY;

        }

        /// <summary>
        /// Updates the Position vector for this camera
        /// </summary>
        /// <param name="time">
        /// A <see cref="System.Double"/> containing the time since the last update
        /// </param>
        protected void UpdateMovement(double time)
        {
            if (m_Keyboard[Key.W] && !m_Keyboard[Key.S])
            {
                Movement.Z = 0;
                Movement.Z -= Speed * (float)time;
            }
            else if (m_Keyboard[Key.S] && !m_Keyboard[Key.W])
            {
                Movement.Z = 0;
                Movement.Z += Speed * (float)time;
            }
            else Movement.Z = 0.0f;

            if (m_Keyboard[Key.A] && !m_Keyboard[Key.D])
            {
                Movement.X = 0.0f;
                Movement.X -= Speed * (float)time;
            }
            else if (m_Keyboard[Key.D] && !m_Keyboard[Key.A])
            {
                Movement.X = 0.0f;
                Movement.X += Speed * (float)time;
            }
            else
                Movement.X = 0.0f;

            if (CameraMode == CamMode.FirstPerson)
            {
                TargetPosition += Vector3.Transform(Movement, Quaternion.Invert(TargetOrientationY));
                TargetPosition = new Vector3(TargetPosition.X, 5, TargetPosition.Z);
            }
            else
                TargetPosition += Vector3.Transform(Movement, Quaternion.Invert(Orientation));
            if (CameraMode != CamMode.FlightCamera)
                Position = TargetPosition;

            Console.WriteLine("Position={0}", Position);
        }

        #endregion

        #endregion

    }
}
