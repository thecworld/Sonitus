using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ClickToColor : EditorWindow {
	
	private Texture2D textureToEdit;
	private Texture2D tempTexture = null;
	private bool txReadable;
	private TextureImporterFormat txFormat;	
	
	private Texture2D Tex1 = new Texture2D(1,1);
	private Texture2D Tex2 = new Texture2D(1,1);
	private Color[] color1;
	private Color[] color2;
	private Color[] color3;
	private List<Color> ColorList = new List<Color>();
	
	private Color[] colors;
	private List<ColorBlock> colorBlocks = new List<ColorBlock>();
	private List<string> textureFormats = new List<string>();
	private bool goodFormat = false;

	public bool autoAdjust = false;
	//	int newRows = 2;
	//	int newColumns = 2;

	private int rows = 2;
	private int columns = 2;
	private int dimension = 64;
	private int gridSize = 2;
	private bool grayScale = true;
	private string fileName = "";
	private string savePath = Application.dataPath + "/../Assets/";
	
	private float winWidth;
	
	[MenuItem ("Window/Click To Color")]
	public static void  ShowWindow () {
		EditorWindow.GetWindow(typeof(ClickToColor));
		EditorWindow.GetWindow(typeof(ClickToColor)).minSize = new Vector2(400,320);
		
	}
	
	void OnEnable () 
	{
		winWidth = EditorWindow.GetWindow<ClickToColor>().position.width;
		//set list of acceptable texture formats
		textureFormats.Clear();
		textureFormats.Add ("ARGB32");
		textureFormats.Add ("RGBA32");
		textureFormats.Add ("RGB24");
		textureFormats.Add ("Alpha8");

		//Default file naming for created textures
		fileName = "CtC_" + Random.Range(0,1000).ToString();
	}
	
	void OnDisable()
	{
		ResetTxSetting(textureToEdit);
	}

	//Create UI interface
	void OnGUI()
	{
		EditorGUILayout.Space();
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.BeginVertical();
		//Set parameter for making a new texture
		gridSize = EditorGUILayout.IntField("Grid Size", gridSize);
		dimension = EditorGUILayout.IntField("Texture Size",dimension);
		grayScale = EditorGUILayout.ToggleLeft("Use Grayscale", grayScale);
		fileName = EditorGUILayout.TextField("Texture File Name",fileName);
		if(GUILayout.Button("Save To..."))
		{
			savePath = EditorUtility.SaveFolderPanel("Save Files To...",savePath,fileName);
		}

		//Auto adjust parameter to make texture a power of 2
		//And make sure that gridsize is a factor of the texture size
		//This prevents artifacts at the edges of color blocks and the texture as whole
		if(dimension > 0)
			dimension = Mathf.ClosestPowerOfTwo(dimension);
		if(dimension > gridSize)
			gridSize = NearestFactor(gridSize, dimension);

		//Create new texture
		if(GUILayout.Button("Create New Texture"))
		{
			rows = gridSize;
			columns = gridSize;

			tempTexture = CreateNewTexture(rows,dimension	);
			NewTexture(tempTexture);
			tempTexture = textureToEdit;
		}

		EditorGUILayout.EndVertical();
		//Slot of previously created texture
		tempTexture = EditorGUILayout.ObjectField("Texture to be edited", tempTexture, typeof(Texture2D), false) as Texture2D;
		EditorGUILayout.EndHorizontal();

		//Check if there is texture to be edited
		if(tempTexture == null)
			return;
		
		//If new texture then set up for edit
		if(tempTexture != textureToEdit)
		{
			NewTexture(tempTexture);
			tempTexture = textureToEdit;
		}
		
		//Check the format of texture
		//Must be readable and be one of the "acceptable" formats
		CheckFormat(textureToEdit);
		
		if(goodFormat)
		{
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();

			//reverts texture to "original state" defined by colors before current edit
			if(GUILayout.Button("Revert to Original"))
			{
				textureToEdit.SetPixels(color3);
				textureToEdit.Apply();
				MatchColors(textureToEdit);
				GetColors(textureToEdit);			
			}   
			EditorGUILayout.EndHorizontal();

			//Update colors in UI
			if(ColorList.Count != rows * columns)
			{
				if(ColorList.Count > rows * columns)
				{
					ColorList.RemoveAt(ColorList.Count - 1);
				}
				else
				{
					for(int i = 0; i < rows*columns; i++)
					{
						ColorList.Add(new Color());
					}
				}
			}
			
			//Create Color Array
			for(int i = 0; i < rows; i++)
			{
				EditorGUILayout.BeginHorizontal();
				
				for(int j = 0; j < columns; j++)
				{
					Color tempColor;
					tempColor = EditorGUILayout.ColorField(ColorList[i + j*rows]);
					
					if(tempColor != ColorList[i + j*rows])
					{
						ReplaceColors(textureToEdit, tempColor, rows - i - 1, j);
						ColorList[i + j*rows] = tempColor;
					} 
				}
				EditorGUILayout.EndHorizontal();
			}
		}
		
		EditorGUILayout.Space();

		//Buttons used to save or cache a version of the colors
		EditorGUILayout.BeginHorizontal();
		if(GUILayout.Button("Save 1"))
		{
			Tex1 = new Texture2D(textureToEdit.width,textureToEdit.width);
			Tex1.SetPixels(colors);
			Tex1.Apply();
			color1 = Tex1.GetPixels();	
		}
		if(GUILayout.Button("Save 2"))
		{
			Tex2 = new Texture2D(textureToEdit.width,textureToEdit.width);
			Tex2.SetPixels(colors);
			Tex2.Apply();
			color2 = Tex2.GetPixels();				
		}
		EditorGUILayout.EndHorizontal();
		
		EditorGUILayout.BeginHorizontal();
		if(Tex1 != null)
			EditorGUI.DrawPreviewTexture(new Rect(winWidth * 0.2f,230 + 18 * rows,75,75),Tex1);
		if(Tex2 != null)
			EditorGUI.DrawPreviewTexture(new Rect(winWidth * 0.65f,230 + 18 * rows,75,75),Tex2);
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginHorizontal();

		//Buttons used to load cached or save version of texure
		if(GUILayout.Button("Load 1"))
		{
			textureToEdit.SetPixels(color1);
			textureToEdit.Apply();
			MatchColors(textureToEdit);			
		}
		if(GUILayout.Button("Load 2"))
		{
			textureToEdit.SetPixels(color2);
			textureToEdit.Apply();
			MatchColors(textureToEdit);			
		}
		EditorGUILayout.EndHorizontal();
	}

	//Sets up the texture and the editor script for new texture
	void NewTexture(Texture2D tempTx)
	{
		if(tempTx == null)
			return;
		
		MakeReadable(tempTx);
		MakeBackUp(tempTx);
		
		//cache current colors for revert
		color3 = tempTx.GetPixels();
		
		MatchColors(tempTx);
		GetColors(tempTx);
		
		int txWidth;
		int txHeight;
		txWidth = tempTx.width;
		txHeight = tempTx.height;
		
		//reset saved textures
		Tex1 = new Texture2D(txWidth,txHeight);
		Tex2 = new Texture2D(txWidth,txHeight);
		color1 = color3;
		color2 = color3;
		Tex1.SetPixels(color1);
		Tex1.Apply();
		Tex2.SetPixels(color2);
		Tex2.Apply();
		
		//will only get here with new texture
		//will reset parameter of old texture
		if(textureToEdit != null)
			ResetTxSetting(textureToEdit);
		
		textureToEdit = tempTx;
	}
	
	//Makes the texture file readable for editing purposes
	void MakeReadable(Texture2D _texture)
	{
		string path;
		path = AssetDatabase.GetAssetPath(_texture);
		
		TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
		//Save setting to reset after disable or unload of texture
		txReadable = importer.isReadable;
		txFormat = importer.textureFormat;
		importer.isReadable = true;
		importer.textureFormat = TextureImporterFormat.RGBA32;
		AssetDatabase.ImportAsset(path);
	}

	//Check to see if the format of the texture is appropriate for editing
	void CheckFormat(Texture2D _texture)
	{
		goodFormat = false;
		
		for(int i = 0; i < textureFormats.Count; i++)
		{
			if(textureFormats[i].ToString() == _texture.format.ToString())
			{
				goodFormat = true;
			}
		}		
		try
		{
			textureToEdit.GetPixels();
		}
		catch (UnityException e)
		{
			//Next line to prevent Unity from throwing occasional errors.
			e.GetType();
			
			goodFormat = false;
		}
		
		//If not good formating then make it good formatting
		if(!goodFormat && _texture != null)
		{
			MakeReadable(_texture);
		}
	}
	
	//Reset readable and format to original
	void ResetTxSetting(Texture2D txTemp)
	{
		if(txTemp == null)
			return;
		
		string path;
		path = AssetDatabase.GetAssetPath(txTemp);
		byte[] pngData = txTemp.EncodeToPNG();
		
		if(pngData != null)
		{
			//Debug.Log("Saving Data");
			File.WriteAllBytes(path,pngData);
			AssetDatabase.Refresh();
		}
		
		TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
		if(path != null)
		{
			//Debug.Log("reseting");
			importer.isReadable = txReadable;
			importer.textureFormat = txFormat;
			AssetDatabase.ImportAsset(path);
		}
	}

	//Searches the texture for different colors
	//Sets up "grid" of colors
	void MatchColors(Texture2D _texture)
	{
		int tempRows = 1 ;
		int tempColumns = 1;
		Color tempColor1;
		Color tempColor2;

		//Gets numbers of rows
		tempColor1 = _texture.GetPixel(0,0);
		for(int i = 0; i < _texture.width; i++)
		{
			tempColor2 = _texture.GetPixel(i,0);
			if(!CompareColor(tempColor1,tempColor2))
			{
				tempRows++;
				tempColor1 = tempColor2;
			}

			if(tempRows > 13)
				break;
		}

		//gets number of columns
		tempColor1 = _texture.GetPixel(0,0);
		for(int i = 0; i < _texture.height; i++)
		{
			tempColor2 = _texture.GetPixel(0,i);
			if(!CompareColor(tempColor1,tempColor2))
			{
				tempColumns++;
				tempColor1 = tempColor2;
			}
			if(tempColumns > 13)
				break;
		}

		//Sets global variables to match findings
		rows = tempRows;
		columns = tempColumns;

		//steps through the grid to find the different colors and stores them on ColorList
		//Steps are based on row and columns numbers as well as the grid being uniform size
		int wFirstStep;
		int hFirstStep;
		int widthStep;
		int heightStep;
		widthStep = Mathf.RoundToInt(_texture.width/tempRows);
		wFirstStep = Mathf.RoundToInt(widthStep/2);
		heightStep = Mathf.RoundToInt(_texture.height/tempColumns);
		hFirstStep = Mathf.RoundToInt(heightStep/2);
		
		ColorList.Clear();
		
		for(int i = 0; i < tempRows; i++)
		{
			for(int j = 0; j < tempColumns; j++)
			{
				int xPos;
				int yPos;
				xPos = wFirstStep + widthStep * i;
				yPos = _texture.height - (hFirstStep + heightStep * j);
				Color tempColor;
				tempColor = _texture.GetPixel(xPos,yPos);
				
				ColorList.Add (tempColor);
			}
		}
	}

	//Compares colors to determine if colors have changed
	bool CompareColor(Color color1, Color color2)
	{		
		if(color1.r != color2.r)
		{
			return false;
		}
		else if(color1.g != color2.g)
		{
			return false;
		}
		else if(color1.b != color2.b)
		{
			return false;
		}
		else if(color1.a != color2.a)
		{
			return false;
		}
		else
		{
			return true;
		}
	}

	//Simply grabs the colors from the texture and caches them in an array
	void GetColors(Texture2D _texture)
	{
		//colors is array of colors
		colors = _texture.GetPixels();
	}

	//Caches the coordinates of colors
	//This was an improvement to prevent erros when two blocks had identical colors
	void GetColorCoords(Texture2D _texture)
	{
		Color tempColor1;
		Color tempColor2;

		tempColor1 = _texture.GetPixel(0,0);
		for(int i = 0; i < _texture.width; i++)
		{
			for(int j = 0; j < _texture.height; j++)
			{
				tempColor2 = _texture.GetPixel(i,j);
				if(!CompareColor(tempColor1,tempColor2))
				{
					foreach(ColorBlock cb in colorBlocks)
					{
						if(tempColor2 == cb.color)
						{
							cb.colorCoordinates.Add (new Vector2(i,j));
							cb.colorIndex.Add(i + j);
							break;
							
						}							
					}

					ColorBlock tempCB = new ColorBlock();
					tempCB.color = tempColor2;
					tempCB.colorCoordinates.Add (new Vector2(i,j));
					tempCB.colorIndex.Add(i+j);
					colorBlocks.Add (tempCB);
					tempCB.indexNum = GetColorIndex(tempColor2);
					//Debug.Log ("New Color ");// + i + " , " +j + " " + tempColor2);
					//ColorList.Add(tempColor2);

					tempColor1 = tempColor2;
				}
				else
				{
					foreach(ColorBlock cb in colorBlocks)
					{
						if(tempColor2 == cb.color)
						{
							cb.colorCoordinates.Add (new Vector2(i,j));
							cb.colorIndex.Add(i + j);
							break;

						}							
					}
				}
			}
		}
	}

	//Helper function to track colors in texture compared to colors displayed in UI
	int GetColorIndex(Color _color)
	{
		for(int i = 0; i < ColorList.Count; i++)
		{
			if(_color == ColorList[i])
				return i;
		}
		return 0;
	}

	//Old replace function that simply replaces one color in the texture with another
	void ReplaceColors(Color oldColor, Color newColor)
	{

		for(int i = 0; i < colors.Length; i++)
		{
			if(colors[i] == oldColor)
			{
				colors[i] = newColor;
			}
		}
		
		textureToEdit.SetPixels(colors);
		textureToEdit.Apply();
		
		//Undo slows down editor window too significantly to use
		///Undo.RecordObject(textureToEdit, "Color Change");
		//		textureToEdit.SetPixels(colors);
		//		textureToEdit.Apply();
	}

	//new improvement replacment that replaces using the coordinates of the colors
	void ReplaceColors(Texture2D _texture,Color newColor,int row, int column)
	{
		int iMin = Mathf.CeilToInt(_texture.width/rows * column);
		int iMax = Mathf.CeilToInt(_texture.width/rows * (column + 1));
		int jMin = Mathf.FloorToInt(_texture.height/columns * row);
		int jMax = Mathf.CeilToInt(_texture.height/columns * (row + 1));

		if(jMax > _texture.height)
			jMax = _texture.height;
		if(iMax > _texture.width)
			iMax = _texture.width;

		for(int i = iMin; i < iMax ; i++)
		{
			for(int j = jMin; j < jMax; j++)
			{
				int coordinate = _texture.width * j + i;
				if(coordinate < colors.Length)
		       	 colors[coordinate] = newColor;
			}
		}
		
		_texture.SetPixels(colors);
		_texture.Apply();
		
		//Undo slows down editor window too significantly to use
		///Undo.RecordObject(textureToEdit, "Color Change");
		//		textureToEdit.SetPixels(colors);
		//		textureToEdit.Apply();
	}



	//Creates new texture based on gridsize and pixel dimensions
	Texture2D CreateNewTexture(int _gridSize, int _size)
	{
		Texture2D newTexture;
		newTexture = new Texture2D(_size,_size,TextureFormat.ARGB32,false);
		newTexture.SetPixels(SetColors(_size));
		colors = newTexture.GetPixels();
		newTexture.Apply(false);

		//sets each pixel color
		for(int i = 0; i < _gridSize; i++)
		{
			for(int j = 0; j < _gridSize; j++)
			{
				float tempValue;
				Color tempColor;

				if(grayScale)
				{
					tempValue = 1f / (_gridSize * _gridSize) * (i + j);
					tempColor = new Color(tempValue,tempValue,tempValue,1f);;
				}
				else
					tempColor = new Color(Random.Range(0f,1f),Random.Range(0f,1f),Random.Range(0f,1f),1f);;

				ReplaceColors(newTexture,tempColor,i,j);
			}
		}

		//setcolors
		byte[] bytes = newTexture.EncodeToPNG();


		//Saves as file
		File.WriteAllBytes(savePath + "/" + fileName +".png",bytes);
		string tempPath;
		tempPath = AssetDatabase.GetAssetPath(newTexture);
		Object.DestroyImmediate(newTexture);
		AssetDatabase.Refresh();

		string [] tempArray;
		tempArray = AssetDatabase.FindAssets(fileName);

		string path;
		path = AssetDatabase.GUIDToAssetPath(tempArray[0]);

		Debug.Log(path);

		newTexture = AssetDatabase.LoadAssetAtPath(path,typeof(Texture2D)) as Texture2D;

		return newTexture;
	}
	//Sets colors for created texture
	Color[] SetColors(int _size)
	{
		Color[] newColors = new Color[_size * _size];
		
		for(int i = 0; i < newColors.Length; i++)
		{
			newColors[i] = new Color(Random.Range(0f,1f),Random.Range(0f,1f),Random.Range(0f,1f),Random.Range(0f,1f));
		}
		
		return newColors;
	}

	//Each time a texture is loaded a backup copy is saved
	void MakeBackUp(Texture2D _texture)
	{
		string tempName;
		tempName = _texture.name + "_backUp";

		//Check if folder exists. If not create it.
		if(!System.IO.Directory.Exists(Application.dataPath + "/../Assets/CtC BackUps/"))
			AssetDatabase.CreateFolder("Assets","CtC BackUps");

		//checks to see if file already is exists if so increments file name
		tempName = GetFileName(tempName, Application.dataPath + "/../Assets/CtC BackUps/", 0);
		byte[] bytes = _texture.EncodeToPNG();			
		File.WriteAllBytes(Application.dataPath + "/../Assets/CtC BackUps/" + tempName +".png",bytes);
		AssetDatabase.Refresh();
	}

	//recursive function to name file with increasing number
	string GetFileName(string _texName,string _path, int _try)
	{
		_try++;

		string tempName;
		tempName = _texName + "_" + _try;
		if(!System.IO.File.Exists(Application.dataPath + "/../Assets/CtC BackUps/" + tempName + ".png"))
		{
			return tempName;
		}
		else
			return GetFileName(_texName,_path,_try);
	}



	//recursive function to get nearest factor
	int NearestFactor(int _factor, int _number)
	{
		if(_number % _factor == 0)
			return _factor;
		else
			return NearestFactor(_factor + 1, _number);
	}

	//class used to store coordinates of different colors blocks
	public class ColorBlock
	{
		public Color color = new Color();
		public int indexNum;
		public List<Vector2> colorCoordinates = new List<Vector2>();
		public List<int> colorIndex = new List<int>();
	}	
}

