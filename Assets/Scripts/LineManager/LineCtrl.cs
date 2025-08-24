using UnityEngine;
using Data;

namespace LineManager
{
    public class LineCtrl : Singleton<LineCtrl>
    {
        [SerializeField] Transform[] pointTransforms;
        private Vector3[] points;  
        [SerializeField] LineRenderer lineRenderer;
        private Transform ballTransform;
        public float scrollSpeed = 1f;
        private Material material;
    
        protected override void CustomAwake()
        {
            base.CustomAwake();
            points = new Vector3[3];
        }
        void OnEnable()
        {
            points = new Vector3[3]; 
            UpdatePointPositions();
            material = lineRenderer.material;
        }
    
        void Update()
        {
            UpdatePointPositions();
            UpdateLinePositions();

            if (lineRenderer.enabled && material != null)
            {
                Vector2 offset = material.mainTextureOffset;
                offset.x += Time.deltaTime * scrollSpeed;
                material.mainTextureOffset = offset;
            }
        }

        void UpdatePointPositions()
        {
            // if (pointTransforms.Length >= 2 && ballTransform != null)
            // {
            //     lineRenderer.positionCount = 3;
            //     lineRenderer.SetPosition(0, pointTransforms[0].position);   // start
            //     lineRenderer.SetPosition(1, ballTransform.position);        // ball ở giữa
            //     lineRenderer.SetPosition(2, pointTransforms[1].position);   // end
            //
            //     Debug.Log($"Line Points: {lineRenderer.GetPosition(0)} -> {lineRenderer.GetPosition(1)} -> {lineRenderer.GetPosition(2)}");
            // }
            if (pointTransforms != null && pointTransforms.Length > 0 && pointTransforms[0] != null)
            {
                points[0] = pointTransforms[0].position;
            }
        
            if (ballTransform != null)
            {
                points[1] = ballTransform.position;
            }
            if (pointTransforms != null && pointTransforms.Length > 1 && pointTransforms[1] != null)
            {
                points[2] = pointTransforms[1].position;
            }
        }

        public void UpdateLinePositions()
        {
            // if (lineRenderer == null || pointTransforms == null) return;
            //
            // lineRenderer.positionCount = pointTransforms.Length;
            // for (int i = 0; i < pointTransforms.Length; i++)
            // {
            //     lineRenderer.SetPosition(i, pointTransforms[i].position);
            //     Debug.Log($"Line point {i}: {pointTransforms[i].position}");
            // }
            lineRenderer.positionCount = points.Length;
            for (int i = 0; i < points.Length; i++)
            {
                lineRenderer.SetPosition(i, points[i]);
            }
        }
    
        public void TurnOnLine()
        {
            lineRenderer.enabled = true;
            UpdatePointPositions();
            UpdateLinePositions();
        }        
        public void TurnOffLine() => lineRenderer.enabled = false;

        public void SetBallTransform(Transform ball)
        {
            ballTransform = ball;
            UpdatePointPositions();
            UpdateLinePositions();
        }
    }
}