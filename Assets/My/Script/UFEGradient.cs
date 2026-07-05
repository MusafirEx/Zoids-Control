using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UFE3D
{
    [AddComponentMenu("UI/Effects/UFE Gradient")]
    public class UFEGradient : BaseMeshEffect
    {
        public enum Type
        {
            Vertical,
            Horizontal,
            DiagonalBottomLeftToTopRight,
            DiagonalBottomRightToTopLeft
        }

        [SerializeField]
        public Type GradientType = Type.Vertical;

        [SerializeField]
        [Range(-1.5f, 1.5f)]
        public float Offset = 0f;

        [SerializeField]
        private Color32 StartColor = Color.white;

        [SerializeField]
        private Color32 EndColor = Color.black;

        private readonly List<UIVertex> vertexList = new List<UIVertex>();

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || helper == null || helper.currentVertCount == 0)
                return;

            vertexList.Clear();
            helper.GetUIVertexStream(vertexList);

            if (vertexList.Count == 0)
                return;

            float leftX = vertexList[0].position.x;
            float rightX = leftX;
            float bottomY = vertexList[0].position.y;
            float topY = bottomY;

            for (int i = 1; i < vertexList.Count; i++)
            {
                Vector3 position = vertexList[i].position;

                if (position.x < leftX)
                    leftX = position.x;
                else if (position.x > rightX)
                    rightX = position.x;

                if (position.y < bottomY)
                    bottomY = position.y;
                else if (position.y > topY)
                    topY = position.y;
            }

            float width = rightX - leftX;
            float height = topY - bottomY;

            UIVertex vertex = default;

            for (int i = 0; i < helper.currentVertCount; i++)
            {
                helper.PopulateUIVertex(ref vertex, i);

                float normalizedX = Mathf.Approximately(width, 0f)
                    ? 0f
                    : (vertex.position.x - leftX) / width;

                float normalizedY = Mathf.Approximately(height, 0f)
                    ? 0f
                    : (vertex.position.y - bottomY) / height;

                float gradientValue;

                switch (GradientType)
                {
                    case Type.Vertical:
                        gradientValue = normalizedY;
                        break;

                    case Type.Horizontal:
                        gradientValue = normalizedX;
                        break;

                    case Type.DiagonalBottomLeftToTopRight:
                        gradientValue = (normalizedX + normalizedY) * 0.5f;
                        break;

                    case Type.DiagonalBottomRightToTopLeft:
                        gradientValue = ((1f - normalizedX) + normalizedY) * 0.5f;
                        break;

                    default:
                        gradientValue = normalizedY;
                        break;
                }

                vertex.color = Color32.Lerp(
                    EndColor,
                    StartColor,
                    gradientValue - Offset
                );

                helper.SetUIVertex(vertex, i);
            }
        }
    }
}
