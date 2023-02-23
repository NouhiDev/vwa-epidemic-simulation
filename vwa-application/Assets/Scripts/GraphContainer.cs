using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

// The core functionality of this script was taken from Code Monkey's graph tutorial series
// --> https://www.youtube.com/watch?v=VsUK9K6UbY4&list=PLzDRvYVwl53tpvp6CP6e-Mrl6dmxs9uhx
// However, it was modified in a way to fit the context.

namespace nouhidev
{
    public class GraphContainer : MonoBehaviour
    {
        [Header("Graph Generation Templates")]
        [SerializeField] private Sprite circleSprite;
        private RectTransform labelTemplateX;
        private RectTransform labelTemplateY;
        private RectTransform dashTemplateX;
        private RectTransform dashTemplateY;

        [Header("Graph Analysis")]
        [SerializeField] private TextMeshProUGUI susText;
        [SerializeField] private TextMeshProUGUI infText;
        [SerializeField] private TextMeshProUGUI recText;
        [SerializeField] private TextMeshProUGUI vacText;
        public List<int> valueList = new List<int>();
        public List<int> valueListS = new List<int>();
        public List<int> valueListR = new List<int>();
        public List<int> valueListV = new List<int>();

        // Private Fields
        private RectTransform graphHolder;
        private List<GameObject> gameObjectList = new List<GameObject>();
        private List<GameObject> labelXs = new List<GameObject>();

        private void Awake()
        {
            graphHolder = transform.Find("Graph Holder").GetComponent<RectTransform>();
            labelTemplateX = graphHolder.Find("Label Template X").GetComponent<RectTransform>();
            labelTemplateY = graphHolder.Find("Label Template Y").GetComponent<RectTransform>();
            dashTemplateX = graphHolder.Find("Dash Template Y").GetComponent<RectTransform>();
            dashTemplateY = graphHolder.Find("Dash Template X").GetComponent<RectTransform>();
        }

        /// <summary>
        /// This is a private method that creates a circle GameObject with an Image component and sets its properties. 
        /// The circle is positioned according to the provided anchoredPosition parameter and is anchored to the bottom-left corner 
        /// of the parent graphHolder RectTransform.
        /// </summary>
        /// <param name="anchoredPosition">A Vector2 representing the anchored position of the circle.</param>
        /// <returns>A GameObject representing the created circle.</returns>
        private GameObject CreateCircle(Vector2 anchoredPosition)
        {
            // Create a new GameObject named "Argument" with an Image component
            GameObject go = new GameObject("Argument", typeof(Image));

            // Set the parent of the game object to graphHolder and disable scaling
            go.transform.SetParent(graphHolder, false);

            // Get a reference to the Image component and set its sprite to circleSprite
            Image circleImage = go.GetComponent<Image>();
            circleImage.sprite = circleSprite;

            // Get a reference to the RectTransform component and set its anchored position and size
            RectTransform rectTransform = go.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = Vector2.one;

            // Set the anchor points to the bottom-left corner of the parent RectTransform
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;

            // Return the newly created circle game object
            return go;
        }

        /// <summary>
        /// The function ShowGraph takes a List of integers, a maximum visible value amount (which defaults to -1), and 
        /// two optional functions to create the X and Y-axis labels. The function creates a line graph on the screen with 
        /// the given data, and labels the X and Y axes based on the provided functions or default values.
        /// </summary>
        /// <param name="valueList">A List of integers representing the y-axis data for the line graph.</param>
        /// <param name="maxVisibleValueAmount">An optional integer representing the maximum number of data points to be displayed on the screen at once. 
        /// If this value is not provided, all data points will be shown.</param>
        /// <param name="getAxisLabelX">An optional function that takes an integer as input and returns a string representing the label 
        /// for the X-axis at that point. If this value is not provided, the default label will be the integer value itself.</param>
        /// <param name="getAxisLabelY">An optional function that takes a float as input and returns a string representing the label 
        /// for the Y-axis at that point. If this value is not provided, the default label will be the rounded integer value of the float.</param>
        public void ShowGraph(List<int> valueList, int maxVisibleValueAmount = -1, Func<int, string> getAxisLabelX = null, Func<float, string> getAxisLabelY = null)
        {
            if (getAxisLabelX == null)
            {
                getAxisLabelX = delegate (int _i) { return _i.ToString(); };
            }

            if (getAxisLabelY == null)
            {
                getAxisLabelY = delegate (float _f) { return Mathf.RoundToInt(_f).ToString(); };
            }

            if (maxVisibleValueAmount <= 0)
            {
                maxVisibleValueAmount = valueList.Count;
            }

            foreach (GameObject go in gameObjectList)
            {
                Destroy(go);
            }
            gameObjectList.Clear();

            float graphHeight = graphHolder.sizeDelta.y;
            float graphWidth = graphHolder.sizeDelta.x;

            float yMaximum = valueList[0];
            float yMinimum = valueList[0];

            for (int i = Mathf.Max(valueList.Count - maxVisibleValueAmount, 0); i < valueList.Count; i++)
            {
                int value = valueList[i];
                if (value > yMaximum)
                {
                    yMaximum = value;
                }
                if (value < yMinimum)
                {
                    yMinimum = value;
                }
            }

            yMaximum = SimulationController.instance.populationSize;
            yMinimum = 0;

            float xSize = graphWidth / maxVisibleValueAmount;

            // This loop iterates over four different lists, and assigns a color to each of them
            // based on their position in the loop.
            for (int z = 0; z < 4; z++)
            {
                // Set the text to move
                TextMeshProUGUI textToMove = (z == 0) ? infText : (z == 1) ? susText : (z == 2) ? recText : vacText;

                // Set the text initial string

                string textToMoveStr = (z == 0) ? "Infectious: " : (z == 1) ? "Susceptible: " : (z == 2) ? "Recovered: " : "Vaccinated: ";

                // Set the value of the text to move
                int textToMoveValue = (z == 0) ? SimulationController.instance.GetIndividualsByState(State.Infectious) :
                    (z == 1) ? SimulationController.instance.GetIndividualsByState(State.Susceptible) : (z == 2) ?
                    SimulationController.instance.GetIndividualsByState(State.Recovered) : SimulationController.instance.GetIndividualsByState(State.Vaccinated);

                // Set the current list based on the value of 'z'
                List<int> valueLists = (z == 0) ? valueList : (z == 1) ? valueListS : (z == 2) ? valueListR : valueListV;

                // Set the color for the current list based on the value of 'z'
                Color graphColor = (z == 0) ? Color.red : (z == 1) ? new Color(0 / 255f, 185 / 255f, 255 / 255f) : (z == 2) ? Color.gray : Color.yellow;

                // Set the first value of the current list based on the value of 'z'
                valueList[0] = (z == 0) ? Mathf.RoundToInt(SimulationController.instance.populationSize * SimulationController.instance.infectiousOnStart) : (z == 1) ? SimulationController.instance.populationSize : valueList[0];

                // If we are on the last list and the simulation is not set to include vaccinations, skip this iteration
                if (z == 3 && !SimulationController.instance.vaccination) continue;

                // Set up variables for creating graph points and labels
                int xIndex = 0;
                GameObject lastCircleGameObject = null;

                // This loop creates the graph points and labels for the current list
                for (int i = Mathf.Max(valueLists.Count - maxVisibleValueAmount, 0); i < valueLists.Count; i++)
                {
                    // Calculate the position of the current point on the graph
                    float xPosition = xSize / 2 + xIndex * xSize;
                    float yPosition = ((valueLists[i] - yMinimum) / (yMaximum - yMinimum)) * graphHeight;

                    // Move the text
                    textToMove.gameObject.transform.SetParent(graphHolder, false);
                    RectTransform textRT = textToMove.gameObject.GetComponent<RectTransform>();
                    textRT.anchoredPosition = new Vector2(xPosition + 20f, yPosition);
                    textRT.anchorMin = Vector2.zero;
                    textRT.anchorMax = Vector2.zero;
                    textToMove.text = textToMoveStr + textToMoveValue.ToString();
                    if (textToMoveValue == 0) textToMove.text = "";

                    // Create a new circle game object at the calculated position and add it to the list of game objects
                    GameObject circleGameObject = CreateCircle(new Vector2(xPosition, yPosition));
                    gameObjectList.Add(circleGameObject);

                    // If this isn't the first point, create a connection between the last point and this one, and add it to the list of game objects
                    if (lastCircleGameObject != null)
                    {
                        GameObject dotConnectionGO = CreateDotConnection(lastCircleGameObject.GetComponent<RectTransform>().anchoredPosition, circleGameObject.GetComponent<RectTransform>().anchoredPosition, graphColor);
                        gameObjectList.Add(dotConnectionGO);
                    }

                    // Update the last circle game object to be the current one
                    lastCircleGameObject = circleGameObject;

                    // Create a label for the x-axis at the current position, and add it to the list of game objects
                    RectTransform labelX = Instantiate(labelTemplateX, graphHolder);
                    labelX.anchoredPosition = new Vector2(xPosition, -10f);
                    labelX.GetComponent<TextMeshProUGUI>().text = getAxisLabelX(i);
                    labelX.gameObject.SetActive(true);
                    gameObjectList.Add(labelX.gameObject);
                    labelXs.Add(labelX.gameObject);

                    // Increment the x-axis index
                    xIndex++;
                }

                int seperatorCount = 10;
                for (int i = 0; i <= seperatorCount; i++)
                {
                    RectTransform labelY = Instantiate(labelTemplateY);
                    labelY.SetParent(graphHolder);
                    labelY.gameObject.SetActive(true);
                    float normalizedValue = i * 1f / seperatorCount;
                    labelY.anchoredPosition = new Vector2(-100f, normalizedValue * graphHeight);
                    labelY.GetComponent<TextMeshProUGUI>().text = getAxisLabelY(yMinimum + (normalizedValue * (yMaximum - yMinimum)));
                    gameObjectList.Add(labelY.gameObject);

                    RectTransform dashY = Instantiate(dashTemplateY);
                    dashY.SetParent(graphHolder);
                    dashY.gameObject.SetActive(true);
                    dashY.anchoredPosition = new Vector2(-4f, normalizedValue * graphHeight);
                    gameObjectList.Add(dashY.gameObject);
                }
            }

            // Corrections

            // Show last time steps in days 
            if (SimulationController.instance.timeStepsPassed > 30) labelXs[labelXs.Count - 1].GetComponent<TextMeshProUGUI>().text = "Day " + (SimulationController.instance.timeStepsPassed).ToString();
        }

        /// <summary>
        /// Creates a game object to represent a connection between two dots on the graph.
        /// </summary>
        /// <param name="dotPositionA">The position of the first dot.</param>
        /// <param name="dotPositionB">The position of the second dot.</param>
        /// <param name="color">The color of the dot connection.</param>
        /// <returns>The game object representing the dot connection.</returns>
        private GameObject CreateDotConnection(Vector2 dotPositionA, Vector2 dotPositionB, Color color)
        {
            // Create a new game object with an Image component to represent the dot connection
            GameObject dotConnectionGO = new GameObject("DotConnection", typeof(Image));

            // Set the parent of the game object to the graph holder, and keep its scale and rotation values unchanged
            dotConnectionGO.transform.SetParent(graphHolder, false);

            // Set the color of the Image component to red.
            dotConnectionGO.GetComponent<Image>().color = color;

            // Calculate the direction and distance between the two dot positions
            Vector2 direction = (dotPositionB - dotPositionA).normalized;
            float distance = Vector2.Distance(dotPositionA, dotPositionB);

            // Set the size and position of the game object based on the distance and direction between the two dots
            RectTransform rt = dotConnectionGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(distance, 3f);
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.anchoredPosition = dotPositionA + direction * distance * 0.5f;
            rt.localEulerAngles = new Vector3(0, 0, GetAngleFromVectorFloat(direction));

            // Return the game object representing the dot connection
            return dotConnectionGO;
        }


        /// <summary>
        /// Calculates the angle in degrees between the provided direction vector and the positive x-axis. 
        /// The function returns this angle value in the range [0, 360) degrees.
        /// </summary>
        /// <param name="direction">A Vector2 representing the direction vector for which to calculate the angle.</param>
        /// <returns>A float value representing the angle in degrees between the direction vector and the positive x-axis. 
        /// The angle value is in the range [0, 360) degrees.</returns>
        float GetAngleFromVectorFloat(Vector2 direction)
        {
            // Normalize the direction vector to ensure its length is 1
            direction.Normalize();

            // Calculate the angle in degrees between the direction vector and the positive x-axis
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Ensure that the angle is in the range [0, 360) degrees
            angle = (angle + 360) % 360;

            return angle;
        }
    }
}