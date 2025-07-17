using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableAvatarCollider : MonoBehaviour
{
    private const string AvatarObjectName = "[Spatial SDK] Editor Local Avatar";
    private const string CapsuleChildName = "Capsule";

    void Start()
    {
        StartCoroutine(WaitAndEnableCollider());
    }

    IEnumerator WaitAndEnableCollider()
    {
        GameObject avatar = null;

        while ((avatar = GameObject.Find(AvatarObjectName)) == null)
        {
            yield return null;
        }

        // wait one more frame
        yield return null;

        Debug.Log("Editor Local Avatar Found");

        Transform capsuleTransform = avatar.transform.Find(CapsuleChildName);
        if (capsuleTransform == null)
        {
            Debug.LogWarning("Capsule Not Found");
            yield break;
        }

        CapsuleCollider col = capsuleTransform.GetComponent<CapsuleCollider>();
        if (col != null)
        {
            col.enabled = true;
            Debug.Log("Avatar Capsule Collider Enabled");
        }
        else
        {
            Debug.LogWarning("Avatar Capsule Collider Not Found");
        }
    }
}
