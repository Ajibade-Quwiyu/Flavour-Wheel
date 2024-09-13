using UnityEngine;
using UnityEngine.EventSystems;

public class ZoomAndPan : MonoBehaviour, IPointerDownHandler, IDragHandler
{
   RectTransform imageRect; // Reference to the image RectTransform
   public RectTransform maskRect;  // Reference to the mask RectTransform

   private Vector2 originalSize;
   private Vector2 originalPosition;
   private Vector3 lastMousePosition;

   public float zoomSpeed = 0.1f;     // Speed of zooming
   public float panSpeed = 1f;        // Speed of panning
   public float minZoom = 1f;
   public float maxZoom = 3f;

   // Specify the tag or name to identify the specific instance
   public string targetTag = "ZoomTarget";

   void Start()
   {
       imageRect = this.GetComponent<RectTransform>();

       // Save the original size and position of the image
       originalSize = imageRect.sizeDelta;
       originalPosition = imageRect.anchoredPosition;
   }

   void Update()
   {
       // Check if this instance has the specified tag
       if (!this.gameObject.CompareTag(targetTag))
       {
           return;
       }

       // Handle zooming
       if (Input.touchCount == 2)
       {
           Touch touch0 = Input.GetTouch(0);
           Touch touch1 = Input.GetTouch(1);

           Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
           Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

           float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
           float currentMagnitude = (touch0.position - touch1.position).magnitude;

           float difference = currentMagnitude - prevMagnitude;

           Zoom(difference * zoomSpeed);
       }
       else if (Input.GetAxis("Mouse ScrollWheel") != 0f)
       {
           float scroll = Input.GetAxis("Mouse ScrollWheel");
           Zoom(scroll * zoomSpeed * 100f);
       }
   }

   public void OnPointerDown(PointerEventData eventData)
   {
       if (!this.gameObject.CompareTag(targetTag)) return;
       lastMousePosition = Input.mousePosition;
   }

   public void OnDrag(PointerEventData eventData)
   {
       if (!this.gameObject.CompareTag(targetTag)) return;

       if (imageRect.localScale.x <= 1f && imageRect.localScale.y <= 1f)
           return;

       Vector3 delta = (Input.mousePosition - lastMousePosition) * panSpeed;
       imageRect.anchoredPosition += new Vector2(delta.x, delta.y);

       // Restrict movement within mask bounds when zoomed in
       ClampToBounds();

       lastMousePosition = Input.mousePosition;
   }

   private void Zoom(float increment)
   {
       if (!this.gameObject.CompareTag(targetTag)) return;

       Vector3 scale = imageRect.localScale;
       scale += Vector3.one * increment;
       scale = Vector3.Max(Vector3.one * minZoom, Vector3.Min(Vector3.one * maxZoom, scale));
       imageRect.localScale = scale;

       // Center the image when zooming out to its original size
       if (scale.x <= 1f)
       {
           imageRect.anchoredPosition = originalPosition;
       }
       else
       {
           // Clamp position when zoomed in
           ClampToBounds();
       }
   }

   private void ClampToBounds()
   {
       if (!this.gameObject.CompareTag(targetTag)) return;

       Vector3 position = imageRect.anchoredPosition;

       float imageWidth = imageRect.rect.width * imageRect.localScale.x;
       float imageHeight = imageRect.rect.height * imageRect.localScale.y;

       float maskWidth = maskRect.rect.width;
       float maskHeight = maskRect.rect.height;

       float minX = (maskWidth - imageWidth) / 2;
       float maxX = -minX;
       float minY = (maskHeight - imageHeight) / 2;
       float maxY = -minY;

       if (imageWidth > maskWidth)
       {
           position.x = Mathf.Clamp(position.x, minX, maxX);
       }
       else
       {
           position.x = Mathf.Clamp(position.x, -maxX, maxX);
       }

       if (imageHeight > maskHeight)
       {
           position.y = Mathf.Clamp(position.y, minY, maxY);
       }
       else
       {
           position.y = Mathf.Clamp(position.y, -maxY, maxY);
       }

       imageRect.anchoredPosition = position;
   }
}