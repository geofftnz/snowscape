using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Snowscape.TerrainRenderer.Renderers.LOD
{
    public class Frustum
    {
        public enum ClippingPlane
        {
            Near = 0,
            Far,
            Left,
            Right,
            Top,
            Bottom
        }

        public enum PointClipResult
        {
            Outside = -1,
            OnPlane = 0,
            Inside = 1
        }

        public enum ObjectClipResult
        {
            TotallyOutside=0,
            PartiallyInside,
            TotallyInside
        }

        Vector4[] clippingPlane = new Vector4[6];

        public Frustum(Matrix4 viewProjection)
        {
                        
        }

        public PointClipResult TestPoint(Vector3 p)
        {
            throw new NotImplementedException();
        }

        public ObjectClipResult TestObject(IEnumerable<Vector3> ps)
        {
            throw new NotImplementedException();
        }


                        
    }
}
