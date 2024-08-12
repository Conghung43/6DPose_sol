using System.Collections;
using System.Collections.Generic;
using UnityEngine;


    public class Line : MonoBehaviour
    {
        public GameObject sphere;
        public Guidle bezierCurve;
        public int frequency = 1;
        public int step = 20;
        public List<Transform> items;

        private bool _isShow;
        private void Start()
        {
            for (int i = 0; i < step; i++)
            {
                GameObject item = Instantiate(sphere, null,true);
                items.Add(item.transform);
                Renderer objectRenderer = item.GetComponent<Renderer>();

                if (objectRenderer != null)
                {
                    // Get the current material of the object
                    Material material = objectRenderer.material;

                    // Create a new material instance (to avoid changing the shared material)
                    material = new Material(material);

                    // Set the opacity of the material
                    Color color = material.color;
                    color.a = 1 - i * 0.03f; // Set the alpha component
                    material.color = color;

                    // Assign the modified material back to the object
                    objectRenderer.material = material;
                }
            } 
        }

        public void SetHideLine()
        {
            _isShow = false;
        }

        public void SetStartAndHideLine(Transform start,float second)
        {
            bezierCurve.SetStartPoint(start);
            StartCoroutine(HideCoroutine(second));
        }

        IEnumerator HideCoroutine(float second)
        {
            yield return new WaitForSeconds(second);
            SetHideLine();
        }
        

        public void SetShowLine()
        {
            _isShow = true;
        }

        private void Update()
        {
            if (_isShow)
            {
                ShowLine();
            }
            else
            {
                HideLine();
            }
            
        }

        private void HideLine()
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].gameObject.SetActive(false);
            }
        }

        private void ShowLine()
        {
            float stepSize = frequency * step;
            if (stepSize == 1)
            {
                stepSize = 1f / stepSize;
            }
            else
            {
                stepSize = 1f / (stepSize - 1);
            }

            for (int p = 0, f = 0; f < frequency; f++)
            {
                for (int i = 0; i < step; i++, p++)
                {
                    Vector3 position = bezierCurve.GetPoint(p * stepSize);
                    if (i >= items.Count)
                    {
                        GameObject item = Instantiate(sphere,null);
                        items.Add(item.transform);
                    }
                    items[i].position = position;
                    items[i].gameObject.SetActive(true);
                }
            }

            if (step < items.Count)
            {
                for (int i = step; i < items.Count; i++)
                {
                    items[i].gameObject.SetActive(false);
                }
            }
        }
    }
