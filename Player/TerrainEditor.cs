using UnityEngine;

public class TerrainEditor : MonoBehaviour
{
    // public Transform Camera;
    //public Vector2 CameraRotation;
    //public float CameraRotateSpeed;
    //public float CameraMoveSpeed;
    Vector3 CursorPos;
    public int Size;
    float SizeFloat;
    private void Start()
    {
        Application.targetFrameRate = 60;
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void Update()
    {
        //CameraRotation += new Vector2(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X")) * CameraRotateSpeed * Time.deltaTime;
        //Camera.transform.rotation = Quaternion.Euler(new Vector3(CameraRotation.x, CameraRotation.y, 0f));
        //Camera.transform.position += (Camera.forward * Input.GetAxis("Vertical") + Camera.right * Input.GetAxis("Horizontal") + transform.up * Input.GetAxis("UpDown")) * Time.deltaTime * CameraMoveSpeed;
        SizeFloat = Mathf.Clamp(SizeFloat + Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * 100f, 1, 5);
        Size = Mathf.RoundToInt(SizeFloat);
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 500f))
        {
            Vector3 Raw = hit.point;
            CursorPos = new Vector3Int(Mathf.RoundToInt(Raw.x), Mathf.RoundToInt(Raw.y), Mathf.RoundToInt(Raw.z));
            //Do away with the system of only doing this on one chunk
            //Make EVERY CHUNK check for hits.
            //(Optimize of course)
            Bounds bounds = new Bounds(CursorPos, Vector3.one * Size);
            if (hit.transform.gameObject.GetComponent<Chunk>())
            {
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    ChunkCreator.Singleton.ModifyChunks(bounds, true);
                }
                if (Input.GetKeyDown(KeyCode.Mouse1))
                {
                    ChunkCreator.Singleton.ModifyChunks(bounds, false);
                }
            }
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(CursorPos, Vector3.one * Size);
    }
}
