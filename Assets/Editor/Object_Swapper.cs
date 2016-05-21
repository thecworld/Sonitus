using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class Object_Swapper : EditorWindow {


	public GameObject prefabToReplace;
	public string searchString;
	public GameObject prefabToPlace;
	List<GameObject> ListToReplace = new List<GameObject>();
	GameObject nextGO;
	bool hasList = false;
	public bool matchRotation;
	public bool matchScale;
    public float randomPercent = 1.0f;
    bool revert = false;

	Vector2 scrollPos;
	int num = 0;

	[MenuItem ("Window/Object Swapper")]
	public static void  ShowWindow () {
		EditorWindow.GetWindow(typeof(Object_Swapper));
	}

	void OnGUI()
	{
		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();

		searchString = EditorGUILayout.TextField("Name to Search",searchString);

		//prefabToReplace = EditorGUILayout.ObjectField("GO to Remove", prefabToReplace, typeof(GameObject),false) as GameObject;
		prefabToPlace = EditorGUILayout.ObjectField("GO to Place", prefabToPlace, typeof(GameObject),false) as GameObject;

		matchRotation = EditorGUILayout.ToggleLeft("Match Rotation", matchRotation);
		matchScale = EditorGUILayout.ToggleLeft("Match Scale", matchScale);
        randomPercent = EditorGUILayout.Slider(randomPercent, 0.0f, 1.0f);



        if (revert)
        {
            if (GUILayout.Button("Revert to Prefab Instance"))
            {
                RevertPrefab();
            }
        }
        revert = EditorGUILayout.ToggleLeft("Use Prefab Revert", revert);


        GUILayout.BeginHorizontal();

		if(GUILayout.Button("Get List"))
		{
			GetList();
			if(ListToReplace.Count > 0)
			{
				hasList = true;
			}
			else
			{
				hasList = false;
			}
		}

		if(GUILayout.Button("Zoom to Next"))
		{
			ZoomToNext();
		}
		if(GUILayout.Button("Skip"))
		{
			SkipGO();
		}
		GUILayout.EndHorizontal();

		if(hasList && ListToReplace.Count > 0)
		{
			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Replace"))
			{
				if(nextGO == null)
				{
					nextGO = ListToReplace[0];
				}
				ReplaceGO(nextGO);
			}

			if(GUILayout.Button("Replace All"))
			{
				for(int i = ListToReplace.Count - 1; i > -1; i--)
				{
					ReplaceGO(ListToReplace[i]);
				}

			}
			GUILayout.EndHorizontal();
		}

		GUILayout.EndVertical();

		scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

		//Listing of items goes here
		EditorGUILayout.LabelField("Matching GameObjects");

		for(int i = 0; i < ListToReplace.Count; i++)
		{
			EditorGUILayout.BeginHorizontal();
			ListToReplace[i] = EditorGUILayout.ObjectField(i.ToString(), ListToReplace[i], typeof(GameObject),false) as GameObject;

			if(GUILayout.Button("X"))
			{
				ListToReplace.RemoveAt(i);
			}
			EditorGUILayout.EndHorizontal();
		}

		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndHorizontal();
	}

	void GetList()
	{
		GameObject[] tempList;
		num = 0;

		ListToReplace.Clear();

		tempList = (GameObject[]) FindObjectsOfType(typeof(GameObject));

		foreach(GameObject GO in tempList)
		{
			if(GO.name.Contains(searchString))
			{
                float tempRand;
                tempRand = UnityEngine.Random.Range(0.0f, 1.0f);
                if(randomPercent > tempRand)
                    ListToReplace.Add(GO);
			}
		}

		if(ListToReplace.Count > 0)
		{
			hasList = true;
		}
		else
		{
			hasList = false;
		}

			Debug.Log("Found : " + ListToReplace.Count);
	}

	void SkipGO()
	{
		ListToReplace.Remove(nextGO);
	}

	void ZoomToNext()
	{
		if(hasList)
		{	
			if(num >= ListToReplace.Count)
			{
				num = 0;
			}

			nextGO = ListToReplace[num];
			num++;
			
			Selection.activeGameObject = nextGO;
			SceneView.lastActiveSceneView.FrameSelected();
		}
	}

	void ReplaceGO(GameObject replaceMe)
	{
		Debug.Log ("Replacing : " + replaceMe.name);
		Transform tempTrans;
		tempTrans = replaceMe.transform;

		Vector3 tempPos;
		Quaternion tempRot;
		Vector3 tempScale;
		Transform parentTrans;
		String tempName;

		tempPos = tempTrans.position;
		tempRot = tempTrans.rotation;
		tempScale = tempTrans.localScale;
		parentTrans = tempTrans.parent;
		tempName = prefabToPlace.name;

		GameObject tempGO;
		tempGO  = PrefabUtility.InstantiatePrefab(prefabToPlace) as GameObject;
		Undo.RegisterCreatedObjectUndo(tempGO, "Created GO");

		tempGO.transform.position = tempPos;
		tempGO.transform.rotation = tempRot;
		tempGO.transform.localScale = tempScale;
		tempGO.transform.SetParent(parentTrans,true);
		tempGO.name = tempName;

		ListToReplace.Remove(replaceMe);
		Undo.DestroyObjectImmediate(replaceMe);

	}

    void RevertPrefab()
    {
        if(ListToReplace.Count > 0)
        {
            foreach(GameObject item in ListToReplace)
            {
                PrefabUtility.RevertPrefabInstance(item);
            }
        }
    }
}
