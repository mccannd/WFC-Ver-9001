using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelSpace : MonoBehaviour {

	public GameObject phVoxel;
	public GameObject selector;

	private GameObject activeSelector;
	public List<GameObject> phExistingVoxels;
	private GameObject[,,] voxelsGO = new GameObject[5, 5, 5];

	private Vector3Int iDims = new Vector3Int (5, 5, 5);
	private bool[,,] voxels = new bool[5, 5, 5];
	private Vector3 halfDims = new Vector3(2.5f, 2.5f, 2.5f);
	private Vector3 origin = new Vector3(0, 2.5f, 0);

	private Vector3Int selectIdx = new Vector3Int(-1, -1, -1);
	private Vector3Int selectNor = new Vector3Int(-1, 0, 0);

	private float lastCheckTime = 0.0f;


	private float downTime;

	// Use this for initialization
	void Start () {
		activeSelector = Instantiate(selector, idxToPos(new Vector3Int(2, -1, 2)), Quaternion.FromToRotation(new Vector3(0, 0, 1), new Vector3(0, 1, 0)));

	}

	private void clear() {
		foreach (GameObject obj in phExistingVoxels) {
			Destroy (obj);
		}
		for (int x = 0; x < iDims.x; x++) {
			for (int y = 0; y < iDims.y; y++) {
				for (int z = 0; z < iDims.z; z++) {
					voxels [x, y, z] = false;
					voxelsGO = null;
				}
			}
		}

		phExistingVoxels = new List<GameObject> ();
	}

	private void repopulate() {
		for (int x = 0; x < iDims.x; x++) {
			for (int y = 0; y < iDims.y; y++) {
				for (int z = 0; z < iDims.z; z++) {
					if (voxels [x, y, z]) {
						Vector3 pos = idxToPos (new Vector3Int (x, y, z));
						GameObject v = Instantiate (phVoxel, pos, new Quaternion ()) as GameObject;
						voxelsGO [x, y, z] = v;
						phExistingVoxels.Add (v);
					}
				}
			}
		}
	}

	private int clamp(int x, int min, int max) {
		return Mathf.Max (Mathf.Min (max, x), min);
	}

	private float clamp(float x, float min, float max) {
		return Mathf.Max (Mathf.Min (max, x), min);
	}

	private Vector3 idxToPos(Vector3Int idx) {
		return new Vector3 ((float)idx.x - 2.0f, (float)idx.y + 0.5f, (float)idx.z - 2.0f);
	}

	private int sign(float f) {
		return f > 0.0f ? 1 : (f < 0.0f ? -1 : 0);
	}

	private Vector3Int clampedPosToIdx(Vector3 pos) {
		int x = (int)Mathf.Floor (pos.x + 0.5f) + 2;
		int y = (int)Mathf.Floor (pos.y);
		int z = (int)Mathf.Floor (pos.z + 0.5f) + 2;
		return new Vector3Int (clamp(x, 0, iDims.x - 1), clamp(y, 0, iDims.y - 1), clamp(z, 0, iDims.z - 1));
	}



	private Vector3 rayBoxIntersect(Ray r, ref bool success) {
		Vector3 invDir = new Vector3 (1.0f / r.direction.x, 1.0f / r.direction.y, 1.0f / r.direction.z);

		float x0 = (origin.x - halfDims.x - r.origin.x) * invDir.x;
		float x1 = (origin.x + halfDims.x - r.origin.x) * invDir.x;
		float y0 = (origin.y - halfDims.y - r.origin.y) * invDir.y;
		float y1 = (origin.y + halfDims.y - r.origin.y) * invDir.y;
		float z0 = (origin.z - halfDims.z - r.origin.z) * invDir.z;
		float z1 = (origin.z + halfDims.z - r.origin.z) * invDir.z;

		float tmin = Mathf.Max (Mathf.Max(Mathf.Min(x0, x1), Mathf.Min(y0, y1)), Mathf.Min (z0, z1));
		float tmax = Mathf.Min (Mathf.Min(Mathf.Max(x0, x1), Mathf.Max(y0, y1)), Mathf.Max (z0, z1));

		if (tmin > tmax || tmax < 0.0f) {
			success = false;
			return new Vector3 ();
		}

		success = true;
		return (tmin * r.direction) + r.origin;
	}

	private bool inBounds(Vector3Int idx) {
		return idx.x >= 0 && idx.x < iDims.x && idx.y >= 0 && idx.y < iDims.y && idx.z >= 0 && idx.z < iDims.z;
	}

	private float bound(float s, float ds) {
		if (ds < 0) {
			ds = -ds;
			s = -s;
		}
		s = (s % 1.0f + 1.0f) % 1.0f;
		return (1.0f - s) / ds;
	}


	private void mouseSelect() {
		Ray r = Camera.main.ScreenPointToRay (Input.mousePosition);
		Debug.Log("Ray Direction: " + r.direction.x + ", " + r.direction.y + ", " + r.direction.z);
		bool hit = false;
		Vector3 coll = rayBoxIntersect (r, ref hit);
		if (!hit) {
			Debug.Log ("Missed");
			selectIdx = new Vector3Int (-1, -1, -1);
			return;
		}
		Debug.Log(coll.x + ", " + coll.y + ", " + coll.z);

		Vector3Int currentIdx = clampedPosToIdx (coll);

		// threshold step direction
		Vector3Int step = new Vector3Int(sign(r.direction.x), sign(r.direction.y), sign(r.direction.z));
		Vector3 delta = new Vector3(step.x / r.direction.x, step.y / r.direction.y, step.z / r.direction.z);
		Vector3 bounds = new Vector3 (bound ((float)currentIdx.x, r.direction.x), bound ((float)currentIdx.y, r.direction.y), bound ((float)currentIdx.z, r.direction.z));


		while (currentIdx.x >= 0 && currentIdx.x < iDims.x
		      && currentIdx.y >= 0 && currentIdx.y < iDims.y
		      && currentIdx.z >= 0 && currentIdx.z < iDims.z) {

			if (voxels [currentIdx.x, currentIdx.y, currentIdx.z]) break;

			if (bounds.x < bounds.y) {
				if (bounds.x < bounds.z) {
					currentIdx.x += step.x;
					bounds.x += delta.x;

					selectNor = new Vector3Int (-step.x, 0, 0);
				} else {
					currentIdx.z += step.z;
					bounds.z += delta.z;

					selectNor = new Vector3Int (0, 0, -step.z);
				}
			} else {
				if (bounds.y < bounds.z) {
					currentIdx.y += step.y;
					bounds.y += delta.y;

					selectNor = new Vector3Int (0, -step.y, 0);
				} else {
					currentIdx.z += step.z;
					bounds.z += delta.z;

					selectNor = new Vector3Int (0, 0, -step.z);
				}
			}
		}
		activeSelector.transform.position = idxToPos (currentIdx);
		activeSelector.transform.rotation = Quaternion.FromToRotation (new Vector3 (0, 0, 1), new Vector3 (selectNor.x, selectNor.y, selectNor.z));
		selectIdx = currentIdx;
	}

	private void placeVoxel() {
		Vector3Int loc = selectIdx + selectNor;
		Vector3Int selectXZ = new Vector3Int (selectIdx.x, 0, selectIdx.z);
		if (inBounds (loc) && inBounds(selectXZ)) {
			if (voxels [loc.x, loc.y, loc.z]) return;

			voxels [loc.x, loc.y, loc.z] = true;
			Vector3 pos = idxToPos (loc);
			GameObject v = Instantiate (phVoxel, pos, new Quaternion ()) as GameObject;
			voxelsGO [loc.x, loc.y, loc.z] = v;
		}
	}

	private void removeVoxel() {
		Vector3Int loc = selectIdx;
		if (inBounds (loc) && voxels[loc.x, loc.y, loc.z]) {

			voxels [loc.x, loc.y, loc.z] = false;
			GameObject v = voxelsGO [loc.x, loc.y, loc.z];
			Destroy (v);
			voxelsGO [loc.x, loc.y, loc.z] = null;
		}
	}

	// Update is called once per frame
	void Update () {
		if (Time.time - lastCheckTime > 0.5f)
			mouseSelect (); // update selected area

		if (Input.GetMouseButtonDown (0) || Input.GetMouseButtonDown (1)) {
			downTime = Time.time;
		}
		if (Input.GetMouseButtonUp (0)) {
			if (Time.time - downTime < 0.2f) {
				//clear ();
				//Vector3 nor = new Vector3 ();
				//mouseSelect ();
				//repopulate ();
				placeVoxel();
			}
		}

		if (Input.GetMouseButtonUp (1)) {
			if (Time.time - downTime < 0.2f) {
				//clear ();
				//Vector3 nor = new Vector3 ();
				//mouseSelect ();
				//repopulate ();
				removeVoxel();
			}
		}
	}
		

	Vector3Int raycast() {
		return new Vector3Int (0, 0, 0);
	}
}
