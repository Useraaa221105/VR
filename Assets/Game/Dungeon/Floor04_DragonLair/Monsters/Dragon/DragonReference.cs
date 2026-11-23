using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>Reference dragon: sculpted skull, bat wing membranes, four jointed limbs and tapered tail.</summary>
    public static class DragonReference
    {
        static readonly Color Red = new Color(.42f,.13f,.075f), Dark = new Color(.23f,.075f,.045f),
            Membrane = new Color(.34f,.12f,.075f), Horn = new Color(.60f,.49f,.32f), Belly = new Color(.55f,.36f,.16f);
        static Transform Find(Monster m,string name)
        {foreach(var t in m.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
        static Transform Replace(Monster m,MonsterAnim a,string name)
        {
            var t=Find(m,name);if(t==null)return null;
            if(t.GetComponent<Renderer>() is Renderer r){r.enabled=false;a.HiddenAlways.Add(r);}
            return ReferenceMesh.Neutral(name+"DragonVisual",t);
        }
        static void Spike(Transform p,Vector3 from,Vector3 bend,Vector3 end,float radius,Color color)
        {ReferenceMesh.Tube("CurvedDragonHorn",p,new[]{from,bend,end},new[]{radius,radius*.55f,.003f},color);}
        public static void Build(Monster m,MonsterAnim a)
        {
            var body=Replace(m,a,"Body");
            var trunk=ReferenceMesh.Node("SculptedTrunk",body,Vector3.zero);trunk.localRotation=Quaternion.Euler(90,0,0);
            ReferenceMesh.Shell("ScaledDragonTrunk",trunk,Vector3.zero,
                new[]{-2.3f,-1.9f,-.8f,.5f,1.8f,2.3f},
                new[]{.05f,.9f,1.4f,1.45f,.85f,.08f},new[]{.05f,.65f,1.1f,1.18f,.75f,.08f},Red,"scale",20);
            for(int j=0;j<7;j++)
            {
                float z=-1.8f+j*.55f;
                ReferenceMesh.Ball("OverlappingVentralScale",body,new Vector3(0,-.75f,z),new Vector3(1.65f,.22f,.68f),Belly);
                Spike(body,new Vector3(0,.95f,z),new Vector3(0,1.45f,z-.12f),new Vector3(0,1.6f,z-.45f),.16f,Dark);
            }
            ReferenceMesh.CombineStatic(body,"BatchedScaledTorso");
            var head=Replace(m,a,"Head");
            ReferenceMesh.Tube("TaperedLongSkull",head,
                new[]{new Vector3(0,0,-.55f),new Vector3(0,.03f,-.1f),new Vector3(0,-.06f,.48f),new Vector3(0,-.09f,.89f)},
                new[]{.19f,.42f,.26f,.18f},Red,"scale",12);
            ReferenceMesh.Ball("BroadBrow",head,new Vector3(0,.18f,.10f),new Vector3(.92f,.27f,.46f),Red);
            var jaw=ReferenceMesh.Node("ArticulatedLowerJaw",head,new Vector3(0,-.18f,-.12f));
            ReferenceMesh.Tube("LowerJawBone",jaw,new[]{new Vector3(0,0,0),new Vector3(0,-.035f,.65f),new Vector3(0,0,.96f)},new[]{.22f,.18f,.13f},Dark,"scale",10);
            for(int s=-1;s<=1;s+=2)
            {
                Spike(head,new Vector3(s*.29f,.22f,-.30f),new Vector3(s*.49f,.55f,-.62f),new Vector3(s*.53f,.62f,-1.0f),.12f,Horn);
                Spike(head,new Vector3(s*.28f,-.04f,-.39f),new Vector3(s*.5f,.05f,-.55f),new Vector3(s*.64f,.10f,-.80f),.09f,Dark);
                ReferenceMesh.Ball("AmberRecessedEye",head,new Vector3(s*.355f,.105f,.18f),new Vector3(.09f,.055f,.14f),new Color(.69f,.48f,.10f));
                ReferenceMesh.Ball("VerticalEyePupil",head,new Vector3(s*.39f,.105f,.19f),new Vector3(.012f,.05f,.03f),new Color(.03f,.022f,.015f));
                ReferenceMesh.Ball("DarkNostril",head,new Vector3(s*.14f,.06f,.75f),new Vector3(.07f,.045f,.06f),Dark);
                for(int j=0;j<5;j++)
                {
                    float z=.15f+j*.145f,x=s*(.23f-j*.015f);
                    Spike(head,new Vector3(x,-.14f,z),new Vector3(x,-.22f,z),new Vector3(x,-.28f,z+.025f),.029f,Horn);
                    Spike(jaw,new Vector3(x,.04f,z+.14f),new Vector3(x,.10f,z+.14f),new Vector3(x,.14f,z+.13f),.023f,Horn);
                }
            }
            ReferenceMesh.CombineStatic(jaw,"BatchedJaw");
            ReferenceMesh.CombineStatic(head,"BatchedDragonSkull",jaw);
            // Batch only static siblings of the moving jaw, not the jaw itself.
            var motion=m.gameObject.AddComponent<DragonReferenceMotion>();motion.Owner=(Dragon)m;motion.HeadPivot=Find(m,"HeadPivot");motion.Jaw=jaw;
            var throat=Find(m,"Throat");
            if(throat!=null)
            {
                var r=throat.GetComponent<Renderer>();r.enabled=false;a.HiddenAlways.Add(r);
                var gold=ReferenceMesh.Neutral("GoldenThroatScaleVisual",throat);
                ReferenceMesh.Shell("GoldenThroatScale",gold,Vector3.zero,new[]{-.20f,-.10f,.16f,.22f},new[]{0f,.24f,.23f,0f},new[]{0f,.06f,.08f,0f},new Color(.72f,.48f,.12f),"iron",10);
            }
            var neckOld=Find(m,"Neck");if(neckOld!=null){var r=neckOld.GetComponent<Renderer>();r.enabled=false;a.HiddenAlways.Add(r);}
            var neck=ReferenceMesh.Tube("FlexibleScaledNeck",m.transform,
                new[]{new Vector3(0,2,1.5f),new Vector3(0,2.4f,2),new Vector3(0,2.9f,2.6f),new Vector3(0,3.2f,3.1f),new Vector3(0,3.3f,3.4f)},
                new[]{.85f,.73f,.55f,.42f,.35f},Red,"scale",12);
            motion.Neck=neck.GetComponent<MeshFilter>().sharedMesh;
            for(int s=-1;s<=1;s+=2)
            {
                var wing=Replace(m,a,s<0?"WingL":"WingR");
                var vertices=new List<Vector3>{
                    new Vector3(0,0,0),new Vector3(s*.6f,1.3f,-.5f),new Vector3(s*2.8f,2.7f,-.6f),
                    new Vector3(s*3.25f,1.6f,-1.1f),new Vector3(s*3.8f,.5f,-2.3f),
                    new Vector3(s*2.4f,.25f,-1.9f),new Vector3(s*1.5f,-.10f,-2.5f),
                    new Vector3(s*.7f,-.2f,-1.8f),new Vector3(0,-.2f,-2.1f),
                    new Vector3(s*1.3f,.75f,-1.15f)};
                var tr=new List<int>();for(int j=0;j<9;j++)tr.AddRange(new[]{9,j,(j+1)%9});
                ReferenceMesh.Draw("ScallopedBatWingMembrane",wing,ReferenceMesh.Make("DragonWing",vertices,tr),Membrane,"wing");
                foreach(int j in new[]{2,4,6,8})
                {
                    var path=j==2?new[]{vertices[0],vertices[1],vertices[2]}:new[]{vertices[0],vertices[1],vertices[2],vertices[j]};
                    var radii=j==2?new[]{.12f,.09f,.018f}:new[]{.12f,.09f,.065f,.018f};
                    ReferenceMesh.Tube("WingFingerBone",wing,path,radii,Dark,"scale",8);
                }
                ReferenceMesh.CombineStatic(wing,"BatchedBatWing");motion.Wings.Add(wing.parent);
                // Rear and opposite front legs are sculpted, not collision walls.
                Leg(m.transform,new Vector3(s*1.12f,.36f,-1.25f),s, false);
                if(s<0)Leg(m.transform,new Vector3(s*1.12f,.44f,2.5f),s,true);
            }
            var paw=Replace(m,a,"Paw");Leg(paw,Vector3.zero,1,true);
            var tail=Replace(m,a,"Tail");
            ReferenceMesh.Tube("LongTaperedDragonTail",tail,
                new[]{new Vector3(0,.3f,1.8f),new Vector3(0,.16f,.9f),new Vector3(.20f,.08f,-.1f),new Vector3(.5f,.02f,-1.1f),new Vector3(.8f,.1f,-2f),new Vector3(.95f,.25f,-2.4f)},
                new[]{.56f,.44f,.30f,.19f,.09f,.01f},Red,"scale",12);
            for(int j=0;j<5;j++)Spike(tail,new Vector3(j*.14f,.25f,1.1f-j*.6f),new Vector3(j*.14f,.62f,1-j*.6f),new Vector3(j*.14f,.7f,.85f-j*.6f),.095f,Dark);
            ReferenceMesh.CombineStatic(tail,"BatchedDragonTail");
        }
        static void Leg(Transform root,Vector3 at,int s,bool front)
        {
            var t=ReferenceMesh.Node("SculptedJointedDragonLeg",root,at);
            ReferenceMesh.Tube("MuscularDragonLimb",t,
                new[]{new Vector3(-s*.2f,front?.9f:.65f,-.2f),new Vector3(s*.20f,.48f,front?-.16f:.30f),new Vector3(s*.10f,-.13f,.15f)},
                new[]{front?.25f:.40f,.22f,.15f},Red,"scale",12);
            ReferenceMesh.Ball("DragonFoot",t,new Vector3(0,-.2f,.23f),new Vector3(.60f,.32f,.76f),Dark);
            for(int j=-1;j<=1;j++)
                Spike(t,new Vector3(j*.18f,-.22f,.52f),new Vector3(j*.18f,-.29f,.73f),new Vector3(j*.19f,-.32f,.89f),.067f,Horn);
            ReferenceMesh.CombineStatic(t,"BatchedDragonLimb");
        }
    }

}