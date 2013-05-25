using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Snowscape.TerrainRenderer.HDR
{
    public class Uncharted2ToneMap : IToneMapper
    {
        private float A = 0.15f;
        private float B = 0.50f;
        private float C = 0.10f;
        private float D = 0.20f;
        private float E = 0.02f;
        private float F = 0.30f;
        private float W = 11.2f;
        private Vector3 CB;
        private Vector3 DE;
        private Vector3 DF;
        private Vector3 EF;

        public float ExposureBias { get; set; }

        public Uncharted2ToneMap()
        {
            CB = new Vector3(C * B);
            DE = new Vector3(D * E);
            DF = new Vector3(D * F);
            EF = new Vector3(E / F);
            ExposureBias = 2.0f;
        }

        private Vector3 Uncharted2Tonemap(Vector3 col)
        {
            Vector3 colA = col * A;

            return (
                        Vector3.Divide(
                            (Vector3.Multiply(col, (colA + CB)) + DE),
                            (Vector3.Multiply(col, (colA + new Vector3(B))) + DF)
                        )
                   ) - EF;
        }

        public Vector3 Tonemap(Vector3 col)
        {
            Vector3 c = Uncharted2Tonemap(col * ExposureBias);
            Vector3 white = Vector3.Divide(Vector3.One, Uncharted2Tonemap(new Vector3(W)));
            return Vector3.Multiply(c, white);
        }
    }
}
