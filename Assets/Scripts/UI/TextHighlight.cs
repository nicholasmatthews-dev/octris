using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TextHighlight : MonoBehaviour
{
    public GameObject m_Canvas;
    GraphicRaycaster m_Raycaster;
    PointerEventData m_PointerEventData;
    bool Hovering = false;
    // Start is called before the first frame update
    void Start()
    {
        m_Raycaster = m_Canvas.GetComponent<GraphicRaycaster>() as GraphicRaycaster;
    }

    // Update is called once per frame
    void Update()
    {
        //Checks to see if the cursor is now over the element
        if (!Hovering)
        {
            if (CheckPointer())
            {
                this.gameObject.transform.localScale = new Vector3(1.2f, 1.2f);
                Hovering = true;
            }
            else
            {
                Hovering = false;
            }
        }
        else
        {
            if (CheckPointer())
            {
                Hovering = true;
            }
            else
            {
                this.gameObject.transform.localScale = new Vector3(1.0f, 1.0f);
                Hovering = false;
            }
        }
    }

    bool CheckPointer()
    {
        m_PointerEventData = new PointerEventData(null);
        m_PointerEventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        m_Raycaster.Raycast(m_PointerEventData, results);
        foreach (RaycastResult x in results)
        {
            if (x.gameObject == this.gameObject)
            {
                return true;
            }
        }
        return false;
    }
}


