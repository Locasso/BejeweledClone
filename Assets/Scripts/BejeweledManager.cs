using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Classe manager do projeto, responsável pelo controle da lógica do jogo.
/// </summary>
public class BejeweledManager : MonoBehaviour
{
	//Events
	public delegate void MatchWarning(); //Evento que dispara para procurar possíveis matchs no grid.
	public static event MatchWarning OnMatchWarning;

	public delegate void AvoidClick(bool turnOff); //Evento disparado para controlar o comportamento do clique em cada item do grid.
	public static event AvoidClick OnAvoidClick;

	[Header("References")]
	[SerializeField] GameObject gridParent; //Referência do objeto pai do grid, que contém todos os nodes/gemas.
	[SerializeField] Sprite[] spriteColorSheet; //Referência para o array de sprites, que definirá a imagem de cada gema.
	[SerializeField] Text scoreValue; //Referência para o texto de score.
	public static BejeweledManager instance; //Instância singleton da classe BejeweledManager.
	GameObject bejeweledObj; //Referência inicial para o modelo de cada objeto gema.

	[Header("Control")]
	[SerializeField] int widhtGrid; //quantidade de linhas e de colunas do grid, respectivamente.
	[SerializeField] int heightGrid; //quantidade de linhas e de colunas do grid, respectivamente.
	[SerializeField] Button firstButton, secondButton; //Referência para cada botão que definirá uma troca no grid.
	int numberGrid; //Recebe o tamanho total de nodes do grid.
	public short hasMatch; //Short que controla o loop de matchs, contando enquanto houver possibilidades de match.
	public static List<GameObject> objMatched = new List<GameObject>(); //Lista estática que guarda todos os objetos que participaram de um match.

	[Header("Temporary Logic Parameters")]
	short tempLine = 0; //Variável temporária para guardar o valor da linha dos botões de match.
	short tempColumn = 0; //Variável temporária para guardar o valor da coluna dos botões de match.
	bool axisHorizontal = false; //Controla em qual direção eu troquei as gemas no grid.
	GameObject result; //Guarda o objeto encontrado ao fazer uma troca no grid.

	[Header("Parameters to control spawn")]
	public Selectable lastObj; //Utilizado para controlar a repetição de cores no spawn.
	public Selectable downObj; //Utilizado para controlar a repetição de cores no spawn.

	public int WidhtGrid { get => widhtGrid; set => widhtGrid = value; }
	public int HeightGrid { get => heightGrid; set => heightGrid = value; }
	public Sprite[] SpriteColorSheet { get => spriteColorSheet; set => spriteColorSheet = value; }

	void Start()
	{
		instance = GetComponent<BejeweledManager>();
		scoreValue = GameObject.Find("score_value").GetComponent<Text>();
		gridParent = FindObjectOfType<GridLayoutGroup>().gameObject;
		numberGrid = widhtGrid * heightGrid;

		StartCoroutine(SpawnGrid());
	}

	/// <summary>
	/// Método responsável pelo spawn do grid, com lógica para não permitir matchs na primeira instância.
	/// </summary>
	/// <returns></returns>
	IEnumerator SpawnGrid()
	{
		short controlLineObj = 0;
		short controlColumnObj = 0;
		bejeweledObj = new GameObject("obj");
		bejeweledObj.SetActive(false);
		bejeweledObj.AddComponent<BejeweledObj>();
		bejeweledObj.AddComponent<Button>();
		bejeweledObj.AddComponent<Image>();
		bejeweledObj.AddComponent<BoxCollider2D>();

		for (int i = 0; i <= numberGrid - 1; i++)
		{
			int rand;

			GameObject obj = Instantiate(bejeweledObj, gridParent.transform);
			obj.SetActive(true);
			yield return null; ;

			if (controlColumnObj > 7)
			{
				controlLineObj++;
				controlColumnObj = 0;
			}

			if (controlColumnObj != 0)
			{
				lastObj = obj.GetComponent<Button>().FindSelectableOnLeft();
			}
			if (controlLineObj > 0)
				downObj = obj.GetComponent<Button>().FindSelectableOnDown();
			rand = Random.Range(0, spriteColorSheet.Length);

			if (controlLineObj < 1 && controlColumnObj != 0)
			{
				while ((int)lastObj.GetComponent<BejeweledObj>().ColorOnSpawn == rand)
					rand = Random.Range(0, spriteColorSheet.Length);
			}
			else if (controlLineObj > 0 && controlColumnObj != 0)
			{
				while ((int)lastObj.GetComponent<BejeweledObj>().ColorOnSpawn == rand ||
					(int)downObj.GetComponent<BejeweledObj>().ColorOnSpawn == rand)
					rand = Random.Range(0, spriteColorSheet.Length);
			}
			else if (controlLineObj > 1 && controlColumnObj == 0)
			{
				while ((int)downObj.GetComponent<BejeweledObj>().ColorOnSpawn == rand)
					rand = Random.Range(0, spriteColorSheet.Length);
			}

			lastObj = null;
			downObj = null;

			//Momento em que crio e delego as configurãções que cada node do grid terá.
			obj.GetComponent<BoxCollider2D>().isTrigger = true;
			obj.GetComponent<BoxCollider2D>().size = obj.GetComponent<RectTransform>().sizeDelta;
			obj.GetComponent<Image>().sprite = spriteColorSheet[rand];
			obj.GetComponent<BejeweledObj>().ColorOnSpawn = (Color)rand;
			obj.GetComponent<BejeweledObj>().LineNode = controlLineObj;
			obj.GetComponent<BejeweledObj>().ColumnNode = controlColumnObj;
			obj.name = ($"obj_{controlLineObj}_{controlColumnObj}");
			controlColumnObj++;
		}

		ControlGridInteractable(false);
		gridParent.GetComponent<GridLayoutGroup>().enabled = false;
	}

	/// <summary>
	/// Void que apenas dispara o evento para controlar o clique nos objetos.
	/// </summary>
	/// <param name="turnOff"></param>
	void ControlGridInteractable(bool turnOff)
	{
		OnAvoidClick?.Invoke(turnOff);
	}

	/// <summary>
	/// Método responsável por reodernar os objetos do grid na hierarquia, 
	/// possibilitando que a leitura dos filhos seja feita de forma correta na matriz.
	/// </summary>
	void ReorderGrid()
	{
		for (int i = 0; i <= WidhtGrid; i++)
			for (int k = 0; k <= HeightGrid; k++)
				for (int j = 0; j <= gridParent.transform.childCount - 1; j++)
					if (gridParent.transform.GetChild(j).GetComponent<BejeweledObj>().LineNode == k &&
						gridParent.transform.GetChild(j).GetComponent<BejeweledObj>().ColumnNode == i)
						gridParent.transform.GetChild(j).SetSiblingIndex(k);
	}

	/// <summary>
	/// Dispara a coroutina do OnClickObj.
	/// </summary>
	public void InvokeOnClick()
	{
		StartCoroutine(OnClickObj());
	}

	/// <summary>
	/// Método que é delegado a cada node/gema do grid. 
	/// Responsável por disparar outras funções como a de match e a de troca entre as gemas.
	/// </summary>
	/// <returns></returns>
	IEnumerator OnClickObj()
	{
		if (firstButton == null)
		{
			firstButton = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
			tempLine = (short)firstButton.GetComponent<BejeweledObj>().LineNode;
			tempColumn = (short)firstButton.GetComponent<BejeweledObj>().ColumnNode;
		}
		else
		{
			secondButton = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();

			if (secondButton.GetComponent<BejeweledObj>().LineNode == firstButton.GetComponent<BejeweledObj>().LineNode &&
				secondButton.GetComponent<BejeweledObj>().ColumnNode == (firstButton.GetComponent<BejeweledObj>().ColumnNode + 1))
			{
				result = firstButton.FindSelectableOnRight().gameObject;
				axisHorizontal = true;
			}
			else if (secondButton.GetComponent<BejeweledObj>().LineNode == firstButton.GetComponent<BejeweledObj>().LineNode &&
				secondButton.GetComponent<BejeweledObj>().ColumnNode == (firstButton.GetComponent<BejeweledObj>().ColumnNode - 1))
			{
				result = firstButton.FindSelectableOnLeft().gameObject;
				axisHorizontal = true;
			}
			else if (secondButton.GetComponent<BejeweledObj>().LineNode == firstButton.GetComponent<BejeweledObj>().LineNode + 1 &&
				secondButton.GetComponent<BejeweledObj>().ColumnNode == firstButton.GetComponent<BejeweledObj>().ColumnNode)
			{
				result = firstButton.FindSelectableOnUp().gameObject;
			}
			else if (secondButton.GetComponent<BejeweledObj>().LineNode == firstButton.GetComponent<BejeweledObj>().LineNode - 1 &&
				secondButton.GetComponent<BejeweledObj>().ColumnNode == firstButton.GetComponent<BejeweledObj>().ColumnNode)
			{
				result = firstButton.FindSelectableOnDown().gameObject;
			}

			if (result != null)
			{

				firstButton.GetComponent<BejeweledObj>().LineNode = result.GetComponent<BejeweledObj>().LineNode;
				firstButton.GetComponent<BejeweledObj>().ColumnNode = result.GetComponent<BejeweledObj>().ColumnNode;

				secondButton.GetComponent<BejeweledObj>().LineNode = tempLine;
				secondButton.GetComponent<BejeweledObj>().ColumnNode = tempColumn;

				StartCoroutine(LerpPosition(firstButton.gameObject, firstButton.transform.position, secondButton.transform.position, axisHorizontal));
				yield return StartCoroutine(LerpPosition(secondButton.gameObject, secondButton.transform.position, firstButton.transform.position, axisHorizontal));

				yield return StartCoroutine(MatchLoop());

				if (objMatched.Count <= 0)
				{
					int tempFirstLine = firstButton.GetComponent<BejeweledObj>().LineNode;
					int tempFirstColumn = firstButton.GetComponent<BejeweledObj>().ColumnNode;
					firstButton.GetComponent<BejeweledObj>().LineNode = tempLine;
					firstButton.GetComponent<BejeweledObj>().ColumnNode = tempColumn;
					secondButton.GetComponent<BejeweledObj>().LineNode = tempFirstLine;
					secondButton.GetComponent<BejeweledObj>().ColumnNode = tempFirstColumn;
					StartCoroutine(LerpPosition(firstButton.gameObject, firstButton.transform.position, secondButton.transform.position, axisHorizontal));
					StartCoroutine(LerpPosition(secondButton.gameObject, secondButton.transform.position, firstButton.transform.position, axisHorizontal));
				}
				else
				{ //Aqui é onde ocorre o loop de matchs no grid, após a primeira disparada do MatchLoop.
					while (hasMatch > 0)
					{
						objMatched.Clear();
						yield return StartCoroutine(MatchLoop());
					}
				}
			}

			firstButton = null;
			secondButton = null;
			result = null;
			axisHorizontal = false;
		}
	}

	/// <summary>
	/// Método responsável por trocar de forma smooth as gemas selecionadas de posição.
	/// </summary>
	/// <param name="obj">Objeto de origem</param>
	/// <param name="posA">Posição inicial</param>
	/// <param name="posB">Posição de destino</param>
	/// <param name="horizontal">Eixo do movimento</param>
	/// <returns></returns>
	IEnumerator LerpPosition(GameObject obj, Vector2 posA, Vector2 posB, bool horizontal)
	{
		float temp = 0f;

		while (temp < 1)
		{
			temp += 1f * Time.deltaTime;
			if (horizontal) posA = new Vector2(Mathf.Lerp(posA.x, posB.x, temp), posA.y);
			else posA = new Vector2(posA.x, Mathf.Lerp(posA.y, posB.y, temp));
			obj.transform.position = posA;
			yield return null;
		}
	}

	/// <summary>
	/// Dispara a coroutine MatchLoop.
	/// </summary>
	void InvokeMatchLoop()
	{
		StartCoroutine(MatchLoop());
	}

	/// <summary>
	/// Método manager que controla toda a lógica atrelada ao match dos objetos no grid.
	/// </summary>
	/// <returns></returns>
	IEnumerator MatchLoop()
	{
		ControlGridInteractable(true);
		hasMatch = 0;

		OnMatchWarning?.Invoke();

		yield return null;

		int score = int.Parse(scoreValue.text);
		int mult = 0;

		foreach (GameObject gem in objMatched)
		{
			gem.gameObject.SetActive(false);
			mult++;
			if (mult >= 4)
				score += 50 * mult;
			else
				score += 50;
			scoreValue.text = score.ToString();
		}

		//Dispara o movimento de quedas pra cada objeto no grid
		if (objMatched.Count > 0)
		{
			for (int i = 0; i <= gridParent.transform.childCount - 1; i++)
			{
				if (gridParent.transform.GetChild(i).gameObject.activeSelf != false)
					yield return StartCoroutine(gridParent.transform.GetChild(i).GetComponent<BejeweledObj>().CheckForMove());
			}
		}

		yield return new WaitForSeconds(1f);

		//Instancia novas gemas
		for (int i = 0; i <= objMatched.Count - 1; i++)
		{
			if (objMatched[i].gameObject.activeSelf == false)
				yield return StartCoroutine(objMatched[i].GetComponent<BejeweledObj>().Reload());

			hasMatch++;
		}

		ReorderGrid();
		ControlGridInteractable(false);
	}
}

/// <summary>
/// Enum que define cada cor de gema.
/// </summary>
public enum Color
{
	RED,
	GREEN,
	YELLOW,
	PURPLE,
	BLUE
}


/// <summary>
/// Classe de configuração de cada node/gema.
/// </summary>
public class BejeweledObj : MonoBehaviour
{
	[SerializeField] Color colorOnSpawn; //Define a cor do objeto.
	[SerializeField] int lineNode, columnNode; //Parâmetros da posição do objeto no grid, sendo lineNode para a linha e columnNode para a coluna.

	public int LineNode { get => lineNode; set => lineNode = value; }
	public int ColumnNode { get => columnNode; set => columnNode = value; }
	public Color ColorOnSpawn { get => colorOnSpawn; set => colorOnSpawn = value; }


	/// <summary>
	/// Método que checa o match com cada objeto adjascente ao que disparou, 
	/// adicionando na lista de matchs caso contenha 3 ou mais nodes.
	/// </summary>
	public void CheckMatch()
	{
		if (columnNode > 0 && columnNode < BejeweledManager.instance.WidhtGrid - 1 &&
			gameObject.GetComponent<Button>().FindSelectableOnRight().gameObject.GetComponent<BejeweledObj>().ColorOnSpawn == this.colorOnSpawn &&
			gameObject.GetComponent<Button>().FindSelectableOnLeft().gameObject.GetComponent<BejeweledObj>().ColorOnSpawn == this.colorOnSpawn)
		{
			BejeweledManager.objMatched.Add(gameObject);
			if (!BejeweledManager.objMatched.Contains(gameObject.GetComponent<Button>().FindSelectableOnRight().gameObject))
				BejeweledManager.objMatched.Add(gameObject.GetComponent<Button>().FindSelectableOnRight().gameObject);
			if (!BejeweledManager.objMatched.Contains(gameObject.GetComponent<Button>().FindSelectableOnLeft().gameObject))
				BejeweledManager.objMatched.Add(gameObject.GetComponent<Button>().FindSelectableOnLeft().gameObject);
		}

		if (lineNode > 0 && lineNode < BejeweledManager.instance.HeightGrid - 1 &&
			gameObject.GetComponent<Button>().FindSelectableOnUp() != null && gameObject.GetComponent<Button>().FindSelectableOnDown() != null &&
			gameObject.GetComponent<Button>().FindSelectableOnUp().gameObject.GetComponent<BejeweledObj>().ColorOnSpawn == this.colorOnSpawn &&
			gameObject.GetComponent<Button>().FindSelectableOnDown().gameObject.GetComponent<BejeweledObj>().ColorOnSpawn == this.colorOnSpawn)
		{
			BejeweledManager.objMatched.Add(gameObject);
			if (!BejeweledManager.objMatched.Contains(gameObject.GetComponent<Button>().FindSelectableOnUp().gameObject))
				BejeweledManager.objMatched.Add(gameObject.GetComponent<Button>().FindSelectableOnUp().gameObject);
			if (!BejeweledManager.objMatched.Contains(gameObject.GetComponent<Button>().FindSelectableOnDown().gameObject))
				BejeweledManager.objMatched.Add(gameObject.GetComponent<Button>().FindSelectableOnDown().gameObject);
		}
	}

	/// <summary>
	/// Método responsável por reciclar os objetos que fizeram parte de um match, e reposicioná-los novamente no topo do grid, 
	/// com nova configuração aleatória.
	/// </summary>
	/// <returns></returns>
	public IEnumerator Reload()
	{
		int rand;
		rand = Random.Range(0, BejeweledManager.instance.SpriteColorSheet.Length);
		Vector2 tempPos = gameObject.transform.position;

		int previousLineNode = lineNode;
		lineNode = BejeweledManager.instance.HeightGrid - 1;

		this.transform.position = new Vector2(tempPos.x, tempPos.y + (gameObject.GetComponent<RectTransform>().sizeDelta.y + 1) * (lineNode - previousLineNode));
		colorOnSpawn = (Color)rand;
		gameObject.GetComponent<Image>().sprite = BejeweledManager.instance.SpriteColorSheet[rand];
		yield return null;
		this.gameObject.SetActive(true);
		yield return StartCoroutine(CheckForMove());
	}

	/// <summary>
	/// Método que checa a possibilidade de cada node cair no grid, com checks feitos por raycast e distância comparada de acordo
	/// com a linha (lineNode) do objeto identificado pelo hit.
	/// </summary>
	/// <returns></returns>
	public IEnumerator CheckForMove()
	{
		if (lineNode > 0)
		{
			Vector2 tempPos = gameObject.transform.position;
			RaycastHit2D[] hit = Physics2D.RaycastAll(transform.position, Vector2.down);
			Collider2D col = gameObject.GetComponent<BoxCollider2D>();

			for (int i = 0; i <= hit.Length - 1; i++)
			{
				if (hit.Length <= 1)
				{
					this.transform.position = new Vector2(transform.position.x,
						tempPos.y - (gameObject.GetComponent<RectTransform>().sizeDelta.y + 1) * lineNode);
					yield return null;

					lineNode -= lineNode;
				}
				else if (hit[i].collider.gameObject != this.gameObject)
				{
					yield return new WaitForFixedUpdate();
					if (hit[i].collider.gameObject.GetComponent<BejeweledObj>().LineNode == lineNode - 1)
						yield break;

					else if (hit[i].collider.gameObject.GetComponent<BejeweledObj>().LineNode < lineNode - 1)
					{
						int mult = lineNode - (hit[i].collider.gameObject.GetComponent<BejeweledObj>().LineNode + 1);
						lineNode = hit[i].collider.gameObject.GetComponent<BejeweledObj>().LineNode + 1;

						this.transform.position = new Vector2(transform.position.x,
							tempPos.y - (gameObject.GetComponent<RectTransform>().sizeDelta.y + 1) * mult);
						yield return null;

						yield break;
					}
				}
			}
		}
	}

	/// <summary>
	/// Método de retorno para a disparada do evento, que adiciona ou remove os Listeners do componente botão de cada node.
	/// </summary>
	/// <param name="turnOff"></param>
	void ClickBehavior(bool turnOff)
	{
		if (turnOff)
			gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
		else
			gameObject.GetComponent<Button>().onClick.AddListener(() => BejeweledManager.instance.InvokeOnClick());
	}

	//Inscrição e remoção de listener para os eventos disparados pelo BejeweledManager.
	private void OnEnable()
	{
		BejeweledManager.OnMatchWarning += CheckMatch;
		BejeweledManager.OnAvoidClick += ClickBehavior;
	}

	private void OnDisable()
	{
		BejeweledManager.OnMatchWarning -= CheckMatch;
		BejeweledManager.OnAvoidClick -= ClickBehavior;
	}
}
