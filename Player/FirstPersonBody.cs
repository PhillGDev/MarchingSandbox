using UnityEngine;

public class FirstPersonBody : MonoBehaviour
{
    public float RotationSpeed, MovementSpeedLimit, MovementSpeedAcel;
    public Rigidbody Body;
    public Transform CameraTarget;
    public static FirstPersonBody Singleton;
    Vector2 Rotation, InputRot, InputMov;
    public Vector3 ForceApplied;
    public void SetConstraints(bool Movement)
    {
        if (!Movement)
        {
            Body.constraints = RigidbodyConstraints.FreezeAll;
        }
        else
        {
            Body.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }
    private void Awake()
    {
        Application.targetFrameRate = 60;
        Singleton = this;
    }
    public bool Look, MoveEnabled;
    public void SetLook(bool Set)
    {
        Look = Set;
    }
    public void SetMove(bool Set)
    {
        MoveEnabled = Set;
    }
    private void Start()
    {
        PlayerCamera.Singleton.SetLock(CameraTarget);
        PlayerCamera.Singleton.SetCursorLock(CursorLockMode.Locked);
    }
    void GetInput()
    {
        if (Look) InputRot = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        else InputRot = Vector3.zero;
        if (MoveEnabled) InputMov = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        else InputMov = Vector2.zero;
    }
    public bool DoInversePos;
    void Move()
    {
        if (Body.constraints == RigidbodyConstraints.FreezeRotation) //Unconstrained movement
        {

            if (Input.GetKeyDown(KeyCode.Space)) Body.AddForce(Vector3.up * 15f * Body.mass);
            if (DoInversePos)
            {
                Transform Playerposition = transform;
                Vector3 offset = new Vector3(Playerposition.position.x, 0f, Playerposition.position.z);
                ChunkCreator.Singleton.transform.position -= offset;
                Playerposition.position -= offset;

            }
        }
        else //Constrained movement (in a vehicle)
        {
            if (DoInversePos)
            {
                Transform Playerposition = transform.parent;
                Vector3 offset = new Vector3(Playerposition.position.x, 0f, Playerposition.position.z); //Object reference not set to an instance of an object.
                ChunkCreator.Singleton.transform.position -= offset;
                Playerposition.position = new Vector3(0f, Playerposition.position.y, 0f);
            }
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Body.AddForce(Vector3.up * 5f * Body.mass, ForceMode.Impulse);
        }
    }
    void ApplyForce()
    {
        Vector3 RelativeVelocity = transform.InverseTransformDirection(Body.velocity);
        Vector3 RequiredForce = transform.forward * (InputMov.y - (RelativeVelocity.z / MovementSpeedLimit)) + transform.right * (InputMov.x - (RelativeVelocity.x / MovementSpeedLimit));
        RequiredForce = Vector3.ClampMagnitude(RequiredForce, MovementSpeedAcel);
        //Debug.DrawLine(transform.position, transform.position + RequiredForce, Color.blue, 1f);
        Body.AddForce(RequiredForce, ForceMode.VelocityChange);
        ForceApplied = RequiredForce;
    }
    void Rotate()
    {
        Rotation.x -= RotationSpeed * InputRot.y * Time.smoothDeltaTime;
        Rotation.y += RotationSpeed * InputRot.x * Time.smoothDeltaTime;
        CameraTarget.transform.localRotation = Quaternion.Euler(new Vector3(Rotation.x, 0f, 0f));
        Body.transform.localRotation = Quaternion.Euler(new Vector3(0f, Rotation.y, 0f));
    }
    private void Update()
    {
        GetInput();
        Move();
        Rotate();
        PlayerCamera.Singleton.UpdateCamera();
    }
    private void FixedUpdate()
    {
        ApplyForce();
    }
}
