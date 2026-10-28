using Aevoria.World;
using UnityEngine;
namespace Aevoria.Presentation
{
    public static class EquipmentGrip012
    {
        // Authored handles run along +Y. Rotate around that shaft first, so
        // pick points and axe edges travel in the forward/downward swing plane.
        public static Quaternion Rotation(ToolKind tool,bool leftHand,bool firstPerson,bool active,float progress,float carryAdjustment=0)
        {
            if(tool==ToolKind.Crate)return Quaternion.AngleAxis(leftHand?90:-90,Vector3.forward)*Quaternion.AngleAxis(firstPerson?60:0,Vector3.right);
            float carry=(firstPerson?110:15)+carryAdjustment,pitch=carry;
            if(active)
            {
                if(progress<.45f)pitch=Mathf.Lerp(carry,firstPerson?100:90,Mathf.SmoothStep(0,1,progress/.45f));
                else if(progress<.80f)pitch=Mathf.Lerp(firstPerson?100:90,140,Mathf.SmoothStep(0,1,(progress-.45f)/.35f));
                else pitch=Mathf.Lerp(140,carry,Mathf.SmoothStep(0,1,(progress-.80f)/.20f));
            }
            float axial=tool==ToolKind.Pickaxe?90:tool==ToolKind.Axe?-90:0;
            // The wrist rolls the fingers across the shaft. Cancel that roll
            // at the mount so the shaft keeps its intended world orientation.
            return Quaternion.AngleAxis(leftHand?90:-90,Vector3.forward)*Quaternion.AngleAxis(leftHand?6:-6,Vector3.forward)*Quaternion.AngleAxis(pitch,Vector3.right)*Quaternion.AngleAxis(axial,Vector3.up);
        }
        public static Quaternion ViewModelRotation(ToolKind tool,bool active,float progress)=>tool==ToolKind.Crate?Quaternion.identity:Quaternion.AngleAxis(-45,Vector3.right)*Quaternion.AngleAxis(90,Vector3.forward)*Rotation(tool,false,true,active,progress);
    }
}
