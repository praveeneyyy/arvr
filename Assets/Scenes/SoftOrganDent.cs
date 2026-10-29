using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class SoftOrganDent : MonoBehaviour
{
    [Header("Dent Settings")]
    public float dentRadius = 0.02f;   // نصف قطر التشوّه (على مقاس الموديل)
    public float dentDepth = 0.02f;   // العمق
    public float recoverySpeed = 7f;   // سرعة الرجوع للشكل الأصلي
    public KeyCode squeezeKey = KeyCode.Mouse0; // الماوس الشمال

    private Mesh deformingMesh;
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;
    private Vector3[] vertexVelocities;

    void Awake()
    {
        // ناخد نسخة من المِش عشان نقدر نعدّل عليه
        var filter = GetComponent<MeshFilter>();
        deformingMesh = filter.mesh;               // mesh = نسخة خاصة بالجيم أوبجكت ده

        originalVertices = deformingMesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];
        vertexVelocities = new Vector3[originalVertices.Length];

        for (int i = 0; i < originalVertices.Length; i++)
            displacedVertices[i] = originalVertices[i];

        // نحدّث الميش كوليدر لأول مرة
        var col = GetComponent<MeshCollider>();
        col.sharedMesh = deformingMesh;
    }

    void Update()
    {
        // نسمح بالتشوّه بس لما نضغط على المفتاح المحدد
        if (Input.GetKeyDown(squeezeKey))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                // مهم جداً: ما نعدّلش غير لو اللي اتضرب بالراي هو نفس الأوبجكت ده
                if (hit.collider.gameObject == gameObject)
                {
                    Vector3 localHitPoint = transform.InverseTransformPoint(hit.point);
                    AddDent(localHitPoint);
                }
            }
        }

        RecoverVertices();
    }

    void AddDent(Vector3 localPoint)
    {
        float sqrRadius = dentRadius * dentRadius;

        for (int i = 0; i < displacedVertices.Length; i++)
        {
            Vector3 pointToVertex = displacedVertices[i] - localPoint;
            float sqrMag = pointToVertex.sqrMagnitude;
            if (sqrMag > sqrRadius)
                continue;

            float distance = Mathf.Sqrt(sqrMag);
            float effect = 1f - distance / dentRadius;

            // ندفن الفيرتكس لجوه شوية
            Vector3 target = originalVertices[i] - pointToVertex.normalized * dentDepth * effect;
            vertexVelocities[i] += (target - displacedVertices[i]);
        }
    }

    void RecoverVertices()
    {
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            vertexVelocities[i] += (originalVertices[i] - displacedVertices[i]) * recoverySpeed * Time.deltaTime;
            displacedVertices[i] += vertexVelocities[i] * Time.deltaTime;
        }

        deformingMesh.vertices = displacedVertices;
        deformingMesh.RecalculateNormals();
        deformingMesh.RecalculateBounds();

        // نحدّث الميش كوليدر بعد التعديل
        var col = GetComponent<MeshCollider>();
        if (col != null)
            col.sharedMesh = deformingMesh;
    }
}
