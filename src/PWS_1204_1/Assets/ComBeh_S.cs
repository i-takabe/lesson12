using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System.Runtime.InteropServices;

struct Particle
{
	public int id;
	public bool active;
	public Vector3 position;
	public Vector3 rotation;
	public float scale;
}

public class ComBeh_S : MonoBehaviour
{
	const int MAX_VERTEX_NUM = 65534;

	[SerializeField, Tooltip("This cannot be changed while running.")]
	int maxInstanceNum = 10000;
	[SerializeField]
	Mesh mesh;
	[SerializeField]
	Shader shader;
	[SerializeField]
	ComputeShader computeShader;
	[SerializeField]
	Texture2D Texture;

	[SerializeField]
	Vector3 range = new Vector3(100.0f, 0, 100.0f);

	private Mesh combineMesh_;
	ComputeBuffer computeBuffer_;
	int emitKernel_;
	List<Material> materials_ = new List<Material>();
	int numParMesh_;
	int meshNum_;
	Color[] colors;

	Vector2 cPos = new Vector2(200, -200);

	Mesh CreateCombineMesh(Mesh mesh, int num)
	{
		Assert.IsTrue(mesh.vertexCount * num <= MAX_VERTEX_NUM);

		var MeshIndices = mesh.GetIndices(0);
		var indexNum = MeshIndices.Length;

		var verteces = new List<Vector3>();
		var indices = new int[num * indexNum];
		var normals = new List<Vector3>();
		var tangents = new List<Vector4>();
		var uv0 = new List<Vector2>();
		var uv1 = new List<Vector2>();

		colors = new Color[num];
		Color c;

		for (int id = 0; id < num; id++)
		{
			verteces.AddRange(mesh.vertices);
			normals.AddRange(mesh.normals);
			tangents.AddRange(mesh.tangents);
			uv0.AddRange(mesh.uv);

			c = new Color(Random.Range(0.1f, 1.0f),
						  Random.Range(0.1f, 1.0f),
						  Random.Range(0.1f, 1.0f), 
						  1);
			colors[id] = c;

			for(int n = 0; n < indexNum; n++)
			{
				indices[id * indexNum + n] = id * mesh.vertexCount + MeshIndices[n];
			}

			for(int n = 0; n <mesh.uv.Length; n++)
			{
				uv1.Add(new Vector2(id, id));
			}
		}

		var combineMesh = new Mesh();
		combineMesh.SetVertices(verteces);
		combineMesh.SetIndices(indices, MeshTopology.Triangles, 0);
		combineMesh.SetNormals(normals);
		combineMesh.RecalculateNormals();
		combineMesh.SetTangents(tangents);
		combineMesh.SetUVs(0, uv0);
		combineMesh.SetUVs(1, uv1);
		combineMesh.RecalculateBounds();
		combineMesh.bounds.SetMinMax(Vector3.one * -100.0f, Vector3.one * 100.0f);

		return combineMesh;
	}

	void OnEnable()
	{
		numParMesh_ = MAX_VERTEX_NUM / mesh.vertexCount;
		meshNum_ = (int)Mathf.Ceil((float)maxInstanceNum / numParMesh_);

		for (int i = 0; i < meshNum_; i++)
		{
			var material = new Material(shader);
			material.SetInt("_IdOffset", numParMesh_ * i);
			materials_.Add(material);
		}

		combineMesh_ = CreateCombineMesh(mesh, numParMesh_);
		computeBuffer_ = new ComputeBuffer(maxInstanceNum, Marshal.SizeOf(typeof(Particle)), ComputeBufferType.Default);

		var initKernel = computeShader.FindKernel("Init");
		emitKernel_ = computeShader.FindKernel("Emit");

		computeShader.SetBuffer(initKernel, "_Particles", computeBuffer_);
		computeShader.SetVector("_Range", range);
		computeShader.Dispatch(initKernel, maxInstanceNum / 8 , 1, 1);
	}

	void OnDisable()
	{
		computeBuffer_.Release();
	}

    // Update is called once per frame
    void Update()
    {
		for(int i= 0; i < meshNum_; i++)
		{
			var material = materials_[i];
			material.SetTexture("_MainTex", Texture);
			material.SetInt("_IdOffset", numParMesh_ * i);
			material.SetBuffer("_Particles", computeBuffer_);			
			material.SetColor("_Color", new Color(0.2f,0.7f,0.3f,1.0f));
			
			Graphics.DrawMesh(combineMesh_, transform.position, transform.rotation, material, 0);					
		}  
    }
}
