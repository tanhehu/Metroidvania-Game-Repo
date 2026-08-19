using UnityEngine;

public class ParalaxBackgroundController : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public Transform layerTransform;
        [Range(0, 50)] public float parallaxFactor;
    }

    public ParallaxLayer[] layers;
    private Vector3 lastCamPos;

    private void Start()
    {
        lastCamPos = transform.position;
    }

    private void LateUpdate()
    {
        Vector3 camDelta = transform.position - lastCamPos;

        foreach (var layer in layers) 
        {
            float x = camDelta.x * layer.parallaxFactor;

            layer.layerTransform.position += new Vector3(x, 0, 0) * Time.deltaTime;
        }

        lastCamPos = transform.position;
    }
}
