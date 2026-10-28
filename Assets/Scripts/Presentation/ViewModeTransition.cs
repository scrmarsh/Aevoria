using Aevoria.World;
using UnityEngine;
namespace Aevoria.Presentation
{
    [DefaultExecutionOrder(1000)]
    public sealed class ViewModeTransition:MonoBehaviour
    {
        private Camera view;private SolidPoint start;private Quaternion rotation;private float elapsed;private bool active,checkedPath,occluded;
        public void Begin(Camera camera,SolidPoint position,Quaternion orientation){view=camera;start=position;rotation=orientation;elapsed=0;active=true;checkedPath=false;}
        private void LateUpdate()
        {
            if(!active||view==null)return;var from=new Vector3((float)(start.X-WorldOrigin.Offset.x),(float)start.Y,(float)(start.Z-WorldOrigin.Offset.z));var target=view.transform.position;var targetRotation=view.transform.rotation;
            if(!checkedPath){var delta=target-from;occluded=delta.sqrMagnitude>.01f&&Physics.SphereCast(from,.15f,delta.normalized,out _,delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);checkedPath=true;}
            elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/.4f);if(t>=1){active=false;return;}
            // A cave roof cannot safely be crossed by a visible camera. Fade
            // between the two safe views when the straight path is obstructed.
            if(occluded){if(t<.5f)view.transform.SetPositionAndRotation(from,rotation);}
            else view.transform.SetPositionAndRotation(Vector3.Lerp(from,target,Mathf.SmoothStep(0,1,t)),Quaternion.Slerp(rotation,targetRotation,Mathf.SmoothStep(0,1,t)));
        }
        private void OnGUI(){if(!active||!occluded)return;int oldDepth=GUI.depth;GUI.depth=-10000;var old=GUI.color;GUI.color=new Color(0,0,0,Mathf.Sin(Mathf.Clamp01(elapsed/.4f)*Mathf.PI));GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;GUI.depth=oldDepth;}
    }
}
