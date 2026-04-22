using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BackgroundScroll : MonoBehaviour
{
    [SerializeField] private Vector2 scrollSpeed = Vector2.down;
    private Image _image;
    private Material _material;
    private Vector2 _offset;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _material = _image.material;
    }

    private void Update()
    {
        _offset += scrollSpeed * Time.deltaTime;
    }

    private void LateUpdate()
    {
        _material.SetTextureOffset("_BaseMap",_offset);
    }
}
