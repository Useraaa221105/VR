using UnityEngine;

namespace Podz
{
    /// <summary>Visual vertex-group rotations only; real MonsterGrabPoint owns all grab/pull rules.</summary>
    [DefaultExecutionOrder(100)]
    public class MandrakeLeafPose : MonoBehaviour
    {
        public Mandrake Owner;
        public MonsterGrabPoint GrabPoint;
        public Transform Foliage;
        [Tooltip("The three original combined foliage meshes. No duplicate renderers or colliders.")]
        public MeshFilter[] LeafMeshes;
        [Tooltip("Vertices per leaf in each original combined mesh: 5, 24, 24. Keep topology in sync.")]
        public int[] VerticesPerLeaf;
        public Vector3[] RestDirections;
        [Range(0f, 1f), Tooltip("0 = rest pose; 1 = gather upwards while really held.")]
        public float GatherStrength = .9f;
        [Range(1f, 30f), Tooltip("Visual convergence rate, 1/seconds. Not the gameplay holding time.")]
        public float Response = 12f;
        Mesh[] _source, _posed;
        Vector3[][] _restVertices, _restNormals, _vertices, _normals;
        Quaternion[] _rotations;
        float _amount;
        bool _invalid;

        bool Prepare()
        {
            if (_source != null) return true;
            if (_invalid) return false;
            if (Foliage == null || LeafMeshes == null || VerticesPerLeaf == null || RestDirections == null ||
                LeafMeshes.Length != 3 || VerticesPerLeaf.Length != 3 || RestDirections.Length != 9) return Invalid();
            for (int i = 0; i < 3; i++)
                if (LeafMeshes[i] == null || LeafMeshes[i].sharedMesh == null || !LeafMeshes[i].sharedMesh.isReadable ||
                    VerticesPerLeaf[i] <= 0 || LeafMeshes[i].sharedMesh.vertexCount != 9 * VerticesPerLeaf[i] ||
                    LeafMeshes[i].sharedMesh.normals.Length != LeafMeshes[i].sharedMesh.vertexCount) return Invalid();
            _source = new Mesh[3]; _posed = new Mesh[3];
            _restVertices = new Vector3[3][]; _restNormals = new Vector3[3][];
            _vertices = new Vector3[3][]; _normals = new Vector3[3][];
            _rotations = new Quaternion[9];
            for (int i = 0; i < 3; i++)
            {
                _source[i] = LeafMeshes[i].sharedMesh;
                _posed[i] = Instantiate(_source[i]); _posed[i].name = _source[i].name + "_HeldInstance"; _posed[i].MarkDynamic();
                _restVertices[i] = _source[i].vertices; _restNormals[i] = _source[i].normals;
                _vertices[i] = new Vector3[_restVertices[i].Length]; _normals[i] = new Vector3[_restNormals[i].Length];
            }
            return true;
        }
        bool Invalid()
        {
            _invalid = true;
            Debug.LogWarning("[MandrakeLeafPose] Топология листьев изменена: поза отключена. Охота/захват не изменяются.", this);
            return false;
        }

        void LateUpdate()
        {
            if (Owner == null || GrabPoint == null || Foliage == null) return;
            bool held = GrabPoint.Held && !Owner.Dead && Owner.State == "Схвачена";
            if (!held && _amount == 0f) return; // no allocations/work for resting plants
            float dt = DTime.Delta; if (dt <= 0f || !Prepare()) return;
            float target = held ? Mathf.Clamp01(GatherStrength) : 0f;
            _amount = Mathf.Lerp(_amount, target, 1f - Mathf.Exp(-Mathf.Clamp(Response, 1f, 30f) * dt));
            if (!held && _amount < .001f) { Restore(); return; }
            Vector3 up = Owner.transform.up;
            Vector3 toHand = GrabPoint.HoldPosition - Foliage.position;
            toHand = Vector3.ProjectOnPlane(toHand, up) + up * Mathf.Max(.25f, Vector3.Dot(toHand, up));
            Vector3 worldDirection = Vector3.RotateTowards(up, toHand.normalized, 25f * Mathf.Deg2Rad, 0f);
            Vector3 localDirection = Foliage.InverseTransformDirection(worldDirection).normalized;
            for (int j = 0; j < 9; j++)
                _rotations[j] = Quaternion.Slerp(Quaternion.identity,
                    Quaternion.FromToRotation(RestDirections[j], localDirection), _amount);
            for (int i = 0; i < 3; i++)
            {
                int groupSize = VerticesPerLeaf[i];
                for (int v = 0; v < _vertices[i].Length; v++)
                {
                    Quaternion rotation = _rotations[v / groupSize];
                    _vertices[i][v] = rotation * _restVertices[i][v];
                    _normals[i][v] = rotation * _restNormals[i][v];
                }
                _posed[i].vertices = _vertices[i]; _posed[i].normals = _normals[i]; _posed[i].RecalculateBounds();
                LeafMeshes[i].sharedMesh = _posed[i]; // same MeshRenderer; never modify the saved source asset
            }
        }
        void Restore()
        {
            _amount = 0;
            if (_source == null) return;
            for (int i = 0; i < 3; i++) if (LeafMeshes[i] != null) LeafMeshes[i].sharedMesh = _source[i];
        }
        void OnDisable() { Restore(); }
        void OnDestroy()
        {
            Restore();
            if (_posed != null) foreach (var mesh in _posed) if (mesh != null) Destroy(mesh);
        }
    }
}
