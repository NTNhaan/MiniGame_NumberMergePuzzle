using UnityEngine;

namespace LineManager
{
    
    public class TrajectoryLine : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private int maxBounce = 3;    
        [SerializeField] private float maxDistance = 20f;

        public void ShowTrajectory(Vector3 startPos, Vector3 initialVelocity)
        {
            Vector3 currentPos = startPos;
            Vector3 direction = initialVelocity.normalized;

            lineRenderer.positionCount = 1;
            lineRenderer.SetPosition(0, currentPos);

            int pointIndex = 1;

            for (int i = 0; i < maxBounce; i++)
            {
                RaycastHit2D hit = Physics2D.Raycast(currentPos, direction, maxDistance, LayerMask.GetMask("Wall"));

                Debug.Log($"[Step {i}] Raycast from {currentPos} dir={direction}");

                if (hit.collider != null)
                {
                    Vector3 hitPoint = hit.point;
                    lineRenderer.positionCount = pointIndex + 1;
                    lineRenderer.SetPosition(pointIndex, hitPoint);

                    Debug.Log($"[Hit {i}] Hit {hit.collider.name} at {hitPoint}, " +
                              $"Normal={hit.normal}, " +
                              $"Reflect={Vector2.Reflect(direction, hit.normal)}");

                    pointIndex++;

                    direction = Vector2.Reflect(direction, hit.normal).normalized;
                    currentPos = hitPoint;
                }
                else
                {
                    Vector3 endPoint = currentPos + direction * maxDistance;
                    lineRenderer.positionCount = pointIndex + 1;
                    lineRenderer.SetPosition(pointIndex, endPoint);

                    Debug.Log($"[End {i}] No hit, draw to {endPoint}");
                    break;
                }
            }
        }

        public void Hide()
        {
            lineRenderer.positionCount = 0;
        }
    }
}