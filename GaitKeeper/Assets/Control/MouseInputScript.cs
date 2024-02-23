using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MotionMatch;


public class MouseInputScript:MonoBehaviour, IMMInput
{


    public Camera mainCamera;

    private Vector2 analogueStick;
    protected DampedTrajectory trajectory;
    public Vector2 AnalogueStick
    {
        get => analogueStick;
        set
        {
            analogueStick = value;
        }
    }

    [SerializeField]
    Transform cameraInWorld;

    [SerializeField]
    Transform fauxRootInWorld;

    private Plane plane;
    protected Vector3 hitPoint;
    protected bool clickMove;
    public virtual bool ClickMove 
    { 
        get => clickMove;
        set
        {
            if (clickMove != value)
            {
                if (value == true)
                {
                    Eignv *= 3;
                    circle.enabled = true;
                }
                else
                {
                    Eignv /= 3;
                    circle.transform.position = new Vector3(hitPoint.x, -0.05f, hitPoint.z);
                }
                clickMove = value;
            }
            
        }
    }

    [SerializeField]
    protected TrackCircle circle;

    Vector2 ProjectedCameraRight
    {
        get
        {
            return new Vector2 { x = cameraInWorld.right.x, y = cameraInWorld.right.z };
        }
    }

    Vector2 ProjectedCameraForward
    {
        get
        {
            return new Vector2 { x = cameraInWorld.forward.x, y = cameraInWorld.forward.z };
        }
    }

    public IEnumerable<Vector2> CurrentTrajectoryAndDirection
    {
        get
        {
            Vector2 curForward = ProjectedCameraForward.normalized;
            Vector2 curRight = ProjectedCameraRight.normalized; 
            return trajectory.CurrentSamples.Select(v=> -v.x*curForward + v.y*curRight);
        }
    }


    private void ClickPerformed()
    {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            float enter = 0.0f;

            if (plane.Raycast(ray, out enter))
            {
                //Get the point that is clicked
                
                hitPoint = cameraInWorld.TransformPoint(mainCamera.transform.InverseTransformPoint(ray.GetPoint(enter)));
                ClickMove = true;
                circle.transform.position = new Vector3(hitPoint.x, 0.05f, hitPoint.z);
                //Move your cube GameObject to the point where you clicked

               }
    }


private void Awake()
    {

        trajectory = new DampedTrajectory();
        plane = new Plane(new Vector3(0, 1, 0), Vector3.zero);
        ClickMove = false;
        
    }

    private void OnDrawGizmos()
    {


    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) ClickPerformed();


        if (ClickMove)
        {
            Vector3 differenceVector = fauxRootInWorld.position - hitPoint;
            if (differenceVector.magnitude < 1.4)
            {
                analogueStick = Vector2.zero;
                ClickMove = false;
            }
            else
            {
                Vector2 projectedDirection = new Vector3(differenceVector.x, differenceVector.z);

                Vector2 curForward = ProjectedCameraForward.normalized;
                Vector2 curRight = ProjectedCameraRight.normalized;

                Vector2 localDirection = new Vector2(Vector2.Dot(projectedDirection, curRight), Vector2.Dot(projectedDirection, curForward));

                analogueStick = Vector2.ClampMagnitude(-localDirection, 1);
            }
        }

        trajectory.TargetVelocity = analogueStick;
        trajectory.UpdateStep(Time.deltaTime);
    }


    public float Eignv { get => trajectory.Eignv; set => trajectory.Eignv = value; }

    public Vector2 AnalogueDirection => analogueStick;

    public Vector2 AnalogueHeading => AnalogueDirection.normalized;

    protected class DampedTrajectory
    {

        readonly IEnumerable<float> timeSamples = MMUtility.LinSpace(1f/3f, 1, 3);

        Vector2 localVelocity;
        Vector2 localAcceleration;

        Vector2 targetVelocity;
        public Vector2 TargetVelocity { get => targetVelocity; set => targetVelocity = value; }

        private float eignv = -3;
        public float Eignv { get => eignv; set => eignv = value; }

        Vector2 c1;
        Vector2 c2;
        Vector2 c3; 

        public IEnumerable<Vector2> CurrentSamples
        {
            get
            {
                c1 = localVelocity - targetVelocity;
                c2 = localAcceleration - c1 * eignv;
                c3 = c2 - c1 * eignv;
                return TrajectoryHorizon.Concat(DirectionHorizon);
            }
        }

        public void UpdateStep(float dt)
        {
            c1 = localVelocity - targetVelocity;
            c2 = localAcceleration - c1 * eignv;
            localVelocity = VelocityStep(dt);
            localAcceleration = AccelerationStep(dt);
        }

        Vector2 VelocityStep(float dt)
        {
            return targetVelocity + (c1 + c2 * dt) * Mathf.Exp(eignv * dt);
        }

        Vector2 AccelerationStep(float dt)
        {
            return (localAcceleration + c2 * eignv * dt) * (float)System.Math.Exp((eignv * dt)); ;
        }

        Vector2 PositionStep(float dt)
        {
            return targetVelocity * dt + (c3 + (c1 * eignv + c2 * eignv * dt - c2) * (float)System.Math.Exp((eignv * dt))) / (eignv * eignv);
        }

        IEnumerable<Vector2> VelocityHorizon
        {
            get
            {
                return timeSamples.Select(t => VelocityStep(t));
            }
        }

        IEnumerable<Vector2> AccelerationHorizon
        {
            get
            {
                return timeSamples.Select(t => AccelerationStep(t));
            }
        }

        IEnumerable<Vector2> TrajectoryHorizon
        {
            get
            {
                return timeSamples.Select(t => PositionStep(t));
            }
        }

        IEnumerable<Vector2> DirectionHorizon
        {
            get
            {
                return timeSamples.Select(t => VelocityStep(t).normalized);
            }
        }
    }
}

