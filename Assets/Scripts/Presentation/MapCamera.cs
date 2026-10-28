using Aevoria.UI;
using Aevoria.World;
using Aevoria.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aevoria.Presentation
{
    // Camera coordinates are Unity (east, elevation, north); geography stays data-only.
    public sealed class MapCamera : MonoBehaviour
    {
        [SerializeField] private float minPitch = -90f;
        [SerializeField] private float maxPitch = 89.7f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 32000f;
        [SerializeField] private float rotationDegreesPerPixel = 0.25f;
        [SerializeField] private float tiltDegreesPerPixel = 0.18f;
        [SerializeField] private float panSensitivity = 1f;
        [SerializeField] private float keyboardAcceleration = 12f;
        [SerializeField] private float movementSensitivity = 1f;
        [SerializeField] private float cameraResponsiveness = 18f;
        [SerializeField] private float minimumTerrainClearance = 3f;

        private IGeography geography;
        private Vector3 targetFocus;
        private Vector3 renderedFocus;
        private Vector3 movementVelocity;
        private float targetYaw;
        private float renderedYaw;
        private float targetPitch = 55f;
        private float renderedPitch = 55f;
        private float targetDistance = 900f;
        private float renderedDistance = 900f;
        private float zoomVelocity;
        private float clearanceLift,lastAdvance=.016f;
        private bool presentationChanged,settlingClearance;
        private bool rotating;
        private bool panning;
        private bool requireButtonRelease;
        private bool initialized;
        private Vector3 frameTranslation;
        private bool composingInput;
        public bool KeyboardMovement {get;set;}=true;
        public bool MiddlePanning {get;set;}=true;
        public bool EdgeScrolling {get;set;}
        public float EdgeZone {get;set;}=16;
        public float EdgeSpeed {get;set;}=1;
        public float MovementSensitivity {get=>movementSensitivity;set=>movementSensitivity=Mathf.Clamp(value,.2f,4);}
        public float MinimumMovementSpeed {get;set;}=60;
        public static bool OverlayBlocked {get;set;}
        public bool TerrainFollowing { get; set; }
        public bool TerrainCollision { get; set; }
        public float ZoomSensitivity { get; set; } = 1.1f;
        public bool ZoomTowardCursor { get; set; } = true;
        public bool ExternalDriver { get; set; }
        public System.Func<float,float,float> PresentationHeight {get;set;}
        public float Clearance {get;private set;}
        public int SafetyChecksBeforeRender {get;private set;}

        public float Distance => renderedDistance;
        public float Pitch => renderedPitch;
        public Vector3 Focus => WorldOrigin.Geographic(renderedFocus);
        private bool originSubscribed;
        private void SubscribeOrigin(){if(originSubscribed)return;WorldOrigin.Shifted+=OriginShifted;originSubscribed=true;}
        private void OriginShifted(Vector3 shift){targetFocus-=shift;renderedFocus-=shift;}
        private void OnDestroy()=>WorldOrigin.Shifted-=OriginShifted;
        private float LocalHeight(float x,float z)=>HeightAt((double)x+WorldOrigin.Offset.x,(double)z+WorldOrigin.Offset.z);

        public void Initialize(IGeography heightSource)
        {
            SubscribeOrigin();
            geography = heightSource;
            maxDistance=Mathf.Max(32000,(float)(heightSource?.Configuration.WidthMeters??0)*1.4f);
            var camera=GetComponent<Camera>();if(camera!=null)camera.farClipPlane=Mathf.Max(180000,maxDistance*2);
            targetFocus = WorldOrigin.Relative(Vector3.zero);
            targetFocus.y = HeightAt(0, 0);
            renderedFocus = targetFocus;
            movementVelocity = Vector3.zero;
            targetYaw = renderedYaw = 0;
            targetPitch = renderedPitch = Mathf.Clamp(55, minPitch, maxPitch);
            targetDistance = renderedDistance = Mathf.Clamp(900, minDistance, maxDistance);
            zoomVelocity=0;clearanceLift=0;
            rotating = panning = false;
            initialized = true;
            Apply();
        }

        private void Start()
        {
            if (!initialized) Initialize(null);
        }

        private void Update()
        {
            if (!initialized || !Application.isFocused || ExternalDriver) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            var mouse = Mouse.current;
            bool overHud = GameInput.Blocked || ApplicationMenu.PointerBlocked || EcologyPresentation.PointerBlocked || SocietyObservation.PointerBlocked || OverlayBlocked || mouse != null && DeveloperHud.IsPointerOverHud(mouse.position.ReadValue());
            frameTranslation=Vector3.zero;
            composingInput=true;
            if (!overHud && !DeveloperHud.IsEditingText) HandleKeyboard(Keyboard.current, dt);
            else movementVelocity = Vector3.zero;
            HandleMouse(mouse, overHud || DeveloperHud.IsEditingText);
            if(!overHud&&!DeveloperHud.IsEditingText&&EdgeScrolling&&mouse!=null)
            {var p=mouse.position.ReadValue();var direction=EdgeVector(p,new Vector2(Screen.width,Screen.height),EdgeZone);var rotation=Quaternion.Euler(0,targetYaw,0);frameTranslation+=rotation*new Vector3(direction.x,0,direction.y)*Speed()*Mathf.Clamp(EdgeSpeed,.2f,3)*dt;}
            targetFocus+=Vector3.ClampMagnitude(frameTranslation,Speed()*Mathf.Max(1,EdgeScrolling?EdgeSpeed:1)*dt);
            composingInput=false;
            Advance(dt);
        }
        public static Vector2 EdgeVector(Vector2 pointer,Vector2 size,float zone)
        {if(pointer.x<0||pointer.y<0||pointer.x>=size.x||pointer.y>=size.y)return Vector2.zero;var v=new Vector2(pointer.x<zone?-1:pointer.x>size.x-zone?1:0,pointer.y<zone?-1:pointer.y>size.y-zone?1:0);return Vector2.ClampMagnitude(v,1);}
        private float Speed()=>Mathf.Clamp(Clearance*.85f,Mathf.Clamp(MinimumMovementSpeed,10,500),Mathf.Max(12000,maxDistance*.2f))*movementSensitivity*(GameInput.Held(GameAction.Sprint)?2.5f:1);
        public void RecenterWorld()
        {targetFocus=WorldOrigin.Relative(Vector3.zero);targetFocus.y=HeightAt(0,0);targetDistance=Mathf.Clamp((float)(geography?.Configuration.WidthMeters??1000)*1.05f,minDistance,maxDistance);targetPitch=68;targetYaw=0;movementVelocity=Vector3.zero;}

        public void MoveHorizontal(Vector2 meters) { targetFocus += new Vector3(meters.x,0,meters.y); }
        public void TrackFocus(Vector3 world){targetFocus=renderedFocus=WorldOrigin.Relative(world);targetDistance=renderedDistance=10;}
        public void SetOrbitTargets(float yaw,float pitch,float distance)
        { targetYaw=yaw; targetPitch=Mathf.Clamp(pitch,minPitch,maxPitch); targetDistance=Mathf.Clamp(distance,minDistance,maxDistance); }

        public void Advance(float dt)
        {
            dt=Mathf.Clamp(float.IsNaN(dt)?0:dt,0,.1f);
            lastAdvance=dt;
            if(!Finite(targetFocus)||!Finite(renderedFocus)||!Finite(targetDistance)||!Finite(renderedDistance)||!Finite(targetPitch)||!Finite(targetYaw))
            {targetFocus=renderedFocus=WorldOrigin.Relative(new Vector3(0,HeightAt(0,0),0));targetDistance=renderedDistance=900;targetYaw=renderedYaw=0;targetPitch=renderedPitch=55;zoomVelocity=0;movementVelocity=Vector3.zero;}
            ClampFocus();
            float focusGround=LocalHeight(targetFocus.x,targetFocus.z);
            if (TerrainFollowing) targetFocus.y = focusGround;
            else targetFocus.y=Mathf.Max(targetFocus.y,focusGround);

            float blend = 1f - Mathf.Exp(-cameraResponsiveness * dt);
            renderedFocus = Vector3.Lerp(renderedFocus, targetFocus, blend);
            renderedYaw = Mathf.LerpAngle(renderedYaw, targetYaw, blend);
            renderedPitch = Mathf.Lerp(renderedPitch, targetPitch, blend);
            renderedDistance = Mathf.SmoothDamp(renderedDistance,targetDistance,ref zoomVelocity,0.10f,Mathf.Infinity,dt);
            Apply();
        }

        private void HandleKeyboard(Keyboard keyboard, float dt)
        {
            if (keyboard == null || DeveloperHud.IsEditingText) return;
            Vector2 direction = Vector2.zero;
            if (KeyboardMovement&&(GameInput.Held(GameAction.Forward))) direction.y++;
            if (KeyboardMovement&&(GameInput.Held(GameAction.Backward))) direction.y--;
            if (KeyboardMovement&&(GameInput.Held(GameAction.Right))) direction.x++;
            if (KeyboardMovement&&(GameInput.Held(GameAction.Left))) direction.x--;
            direction = Vector2.ClampMagnitude(direction, 1f);

            // Ground-relative speed scales with altitude, while remaining usable near terrain.
            float speed = Speed();
            Vector3 forward = Quaternion.Euler(0, targetYaw, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, targetYaw, 0) * Vector3.right;
            Vector3 requestedVelocity = (right * direction.x + forward * direction.y) * speed;
            float acceleration = 1f - Mathf.Exp(-keyboardAcceleration * dt);
            movementVelocity = Vector3.Lerp(movementVelocity, requestedVelocity, acceleration);
            Vector3 movement=movementVelocity*dt;
            if(KeyboardMovement){if(GameInput.Held(GameAction.WorldRise))movement.y+=speed*dt;if(GameInput.Held(GameAction.WorldLower))movement.y-=speed*dt;}
            if(composingInput)frameTranslation+=movement;else targetFocus+=movement;

            if (GameInput.Held(GameAction.RotateLeft)) targetYaw -= 75f * dt;
            if (GameInput.Held(GameAction.RotateRight)) targetYaw += 75f * dt;
            if (GameInput.Held(GameAction.ZoomIn)) ZoomNotches(3f * dt);
            if (GameInput.Held(GameAction.ZoomOut)) ZoomNotches(-3f * dt);
            if (GameInput.Pressed(GameAction.Recenter))
            {
                targetYaw = 0;
                targetPitch = Mathf.Clamp(55, minPitch, maxPitch);
            }
            if (GameInput.Pressed(GameAction.Focus)) FocusUnderPointer(Mouse.current);
        }

        private void HandleMouse(Mouse mouse, bool overHud)
        {
            if (mouse == null) { rotating = panning = false; return; }
            if (requireButtonRelease)
            {
                if (!GameInput.Held(GameAction.Orbit) && !GameInput.Held(GameAction.Pan)) requireButtonRelease = false;
                return;
            }
            var pointer = mouse.position.ReadValue();
            if (overHud || pointer.x < 0 || pointer.y < 0 || pointer.x >= Screen.width || pointer.y >= Screen.height)
            { rotating = panning = false; return; }
            if (GameInput.Pressed(GameAction.Orbit)) rotating = !overHud;
            if (GameInput.Pressed(GameAction.Pan)) panning = !overHud&&MiddlePanning;
            if(!MiddlePanning)panning=false;
            if (!GameInput.Held(GameAction.Orbit)) rotating = false;
            if (!GameInput.Held(GameAction.Pan)) panning = false;

            Vector2 delta = mouse.delta.ReadValue();
            // A focus change or window-edge jump must not teleport the camera.
            delta = Vector2.ClampMagnitude(delta, 140f);
            if (rotating)
            {
                targetYaw += delta.x * rotationDegreesPerPixel;
                targetPitch = Mathf.Clamp(targetPitch - delta.y * tiltDegreesPerPixel,
                    Mathf.Min(minPitch, maxPitch), Mathf.Max(minPitch, maxPitch));
            }
            if (panning)
            {
                // Intersect successive cursor rays with the focus-height plane. No colliders or jumps on press.
                var camera = GetComponent<Camera>();
                var plane = new Plane(Vector3.up, new Vector3(0, renderedFocus.y, 0));
                Ray previous = CameraRays.ScreenLocal(camera,pointer-delta);
                Ray current = CameraRays.ScreenLocal(camera,pointer);
                if (plane.Raycast(previous, out float before) && plane.Raycast(current, out float after))
                {
                    Vector3 shift = previous.GetPoint(before) - current.GetPoint(after);
                    var movement=Vector3.ClampMagnitude(shift * panSensitivity, targetDistance * 0.25f);if(composingInput)frameTranslation+=movement;else targetFocus+=movement;
                }
            }

            if (!overHud)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f)
                {
                    float before = targetDistance;
                    // Input System 1.20 normalizes a native wheel notch to 1 by default.
                    // Synthetic device events must use those same units. Only legacy
                    // Windows platform-specific mode uses 120 units per notch.
                    float notches=wheel;
                    if(InputSystem.settings.scrollDeltaBehavior==InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange &&
                        (Application.platform==RuntimePlatform.WindowsPlayer || Application.platform==RuntimePlatform.WindowsEditor)) notches/=120f;
                    ZoomNotches(notches);
                    if (ZoomTowardCursor)
                    {
                        var plane = new Plane(Vector3.up, renderedFocus);
                        var ray = CameraRays.ScreenLocal(GetComponent<Camera>(),pointer);
                        if (plane.Raycast(ray,out float hit))
                        {
                            Vector3 offset = (ray.GetPoint(hit)-renderedFocus)*(1-targetDistance/before);
                            offset.y=0; targetFocus += Vector3.ClampMagnitude(offset,before*0.7f);
                        }
                    }
                }
            }
        }

        private void FocusUnderPointer(Mouse mouse)
        {
            if (mouse == null || Camera.main == null) return;
            Ray ray = CameraRays.ScreenLocal(Camera.main,mouse.position.ReadValue());
            if (Mathf.Abs(ray.direction.y) < 0.001f) return;
            float t = (LocalHeight(targetFocus.x, targetFocus.z) - ray.origin.y) / ray.direction.y;
            if (t <= 0) return;
            Vector3 point = ray.GetPoint(t);
            // Iterate the height field so focus works on slopes without a physics collider.
            for (int i = 0; i < 3; i++)
            {
                float height = LocalHeight(point.x, point.z);
                t = (height - ray.origin.y) / ray.direction.y;
                if (t <= 0) return;
                point = ray.GetPoint(t);
            }
            targetFocus = point;
        }

        private void Zoom(float change)
        {
            targetDistance = Mathf.Clamp(targetDistance + change, minDistance, maxDistance);
        }
        public void ZoomNotches(float notches)
        {
            if(!Finite(notches))return;
            targetDistance=Mathf.Clamp(targetDistance*Mathf.Exp(-Mathf.Clamp(notches,-8,8)*Mathf.Clamp(ZoomSensitivity,.1f,3)*.22f),minDistance,maxDistance);
            if(notches>0&&geography!=null)targetFocus.y=LocalHeight(targetFocus.x,targetFocus.z);
        }
        private static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        private static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);

        private void ClampFocus()
        {
            // Navigation may cross the finite simulation boundary into scenery.
            // A generous numeric limit avoids loss of float precision far from the world.
            float limit=Mathf.Max(200000,(float)(geography?.Configuration.WidthMeters??0));
            targetFocus.x = Mathf.Clamp(targetFocus.x, -limit-WorldOrigin.Offset.x, limit-WorldOrigin.Offset.x);
            targetFocus.z = Mathf.Clamp(targetFocus.z, -limit-WorldOrigin.Offset.z, limit-WorldOrigin.Offset.z);
        }

        private float HeightAt(double x, double z)
        {
            if(geography==null) return 0;
            float height=geography is ProceduralGeography procedural && !geography.Configuration.Contains(new GeoPoint(x,z))
                ?procedural.SceneryElevationAt(x,z):geography.ElevationAt(x,z);
            return Mathf.Max(geography.Configuration.Terrain.SeaLevel,height);
        }

        public void SetView(GeoPoint focus, float yaw, float pitch, float distance, float? focusElevation = null)
        {
            targetFocus = new Vector3((float)(focus.X-WorldOrigin.Offset.x), 0, (float)(focus.Y-WorldOrigin.Offset.z));
            ClampFocus(); targetFocus.y = focusElevation ?? LocalHeight(targetFocus.x, targetFocus.z);
            renderedFocus = targetFocus; targetYaw = renderedYaw = yaw;
            targetPitch = renderedPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            targetDistance = renderedDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            zoomVelocity=0;clearanceLift=0;
            movementVelocity = Vector3.zero; rotating = panning = false; Apply();
        }

        private void OnApplicationFocus(bool focused)
        {
            rotating = panning = false;
            movementVelocity = Vector3.zero;
            requireButtonRelease = true;
        }

        public void NotifyPresentationChanged()=>presentationChanged=true;
        private void OnEnable(){SubscribeOrigin();Application.onBeforeRender+=BeforeRenderSafety;}
        private void BeforeRenderSafety()
        {if(!initialized||!presentationChanged&&!(ExternalDriver&&settlingClearance))return;presentationChanged=false;SafetyChecksBeforeRender++;Apply();}
        private void OnDisable() { rotating = panning = false;Application.onBeforeRender-=BeforeRenderSafety; }

        private void Apply()
        {
            transform.rotation = Quaternion.Euler(renderedPitch, renderedYaw, 0);
            Vector3 position = renderedFocus - transform.forward * renderedDistance;
            // Surface safety is unconditional in God mode. The optional terrain
            // collision setting adds a wider slope probe; it cannot disable safety.
            double east=(double)position.x+WorldOrigin.Offset.x,north=(double)position.z+WorldOrigin.Offset.z;
            float floor=HeightAt(east,north);
            float presented=PresentationHeight?.Invoke((float)east,(float)north)??floor;
            if(Finite(presented))floor=Mathf.Max(floor,presented);
            if (TerrainCollision)
            {
                for(int i=0;i<8;i++){float a=i*Mathf.PI*.25f;floor=Mathf.Max(floor,HeightAt(east+Mathf.Cos(a)*2,north+Mathf.Sin(a)*2));}
            }
            float requiredLift=Mathf.Max(0,floor+minimumTerrainClearance-position.y);
            float dt=ExternalDriver?Mathf.Min(.1f,Time.unscaledDeltaTime):lastAdvance;
            clearanceLift=Mathf.Max(requiredLift,Mathf.Lerp(clearanceLift,requiredLift,1-Mathf.Exp(-12*dt)));settlingClearance=clearanceLift>requiredLift+.001f;
            position.y+=clearanceLift;
            Clearance=position.y-floor;
            var previousOrigin=WorldOrigin.Offset;if(isActiveAndEnabled)WorldOrigin.Follow(WorldOrigin.Geographic(position));
            position-=WorldOrigin.Offset-previousOrigin;transform.position = position;
            // Keep ground-view depth precision independent of world size. The
            // horizon remains within 180 km; planetary views expand with their
            // current range rather than retaining that far plane after zoom-in.
            var camera=GetComponent<Camera>();if(camera!=null){camera.nearClipPlane=Mathf.Min(Mathf.Min(30,Clearance*.25f),Mathf.Max(CameraRays.PrecisionNear(position),Mathf.Min(renderedDistance*.00004f,Clearance*.08f)));camera.farClipPlane=Mathf.Max(180000,renderedDistance*2,position.y*2);}
        }
    }
}
