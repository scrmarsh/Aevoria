using UnityEngine;
namespace Aevoria.Presentation
{
    // Navigation needs angular rays, not a far-plane inverse projection. At
    // planetary far/near ratios Unity's unprojection can lose the far point.
    public static class CameraRays
    {
        public static float PrecisionNear(Vector3 position)=>Mathf.Max(.05f,Mathf.Max(Mathf.Abs(position.x),Mathf.Max(Mathf.Abs(position.y),Mathf.Abs(position.z)))*.0000004f);
        public static Ray Screen(Camera camera,Vector2 point)
        {
            var rect=camera.pixelRect;
            return Viewport(camera,new Vector2((point.x-rect.x)/rect.width,(point.y-rect.y)/rect.height));
        }
        public static Ray Viewport(Camera camera,Vector2 point)
        {
            var ray=ViewportLocal(camera,point);ray.origin=WorldOrigin.Geographic(ray.origin);return ray;
        }
        public static Ray ScreenLocal(Camera camera,Vector2 point)
        {
            var rect=camera.pixelRect;return ViewportLocal(camera,new Vector2((point.x-rect.x)/rect.width,(point.y-rect.y)/rect.height));
        }
        public static Ray ViewportLocal(Camera camera,Vector2 point)
        {
            var transform=camera.transform;
            if(camera.orthographic)
            {
                var origin=transform.position+transform.right*((point.x*2-1)*camera.orthographicSize*camera.aspect)+transform.up*((point.y*2-1)*camera.orthographicSize)+transform.forward*camera.nearClipPlane;
                return new Ray(origin,transform.forward);
            }
            var projection=camera.projectionMatrix;
            var direction=transform.TransformDirection(new Vector3((point.x*2-1+projection[0,2])/projection[0,0],(point.y*2-1+projection[1,2])/projection[1,1],1)).normalized;
            return new Ray(transform.position+direction*(camera.nearClipPlane/Vector3.Dot(direction,transform.forward)),direction);
        }
    }
}
