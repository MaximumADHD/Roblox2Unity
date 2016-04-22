using UnityEngine;
using System.Collections;

public class LoadCollision : MonoBehaviour
{
    public void Start()
    {
        foreach (MeshRenderer renderer in GameObject.FindObjectsOfType<MeshRenderer>())
        {
			Transform parent = renderer.gameObject.transform.parent;
			if (parent != null)
			{
            	if (renderer.gameObject.transform.parent.Equals(gameObject.transform))
            	{
                	renderer.gameObject.AddComponent<MeshCollider>();
            	}
			}
        }
    }
}
