using System.Collections.Generic;
using UnityEngine;

public class Bus : MonoBehaviour
{
    private const float TwoSeatWidthScale = 0.76f;
    private const float TwoSeatDepthScale = 0.9f;
    private const float SeatIndicatorY = 0.56f;
    private const float SeatIndicatorZ = 0.2f;

    public Color Color { get; private set; }
    public int Capacity { get; private set; }
    public bool IsFull => passengers.Count >= Capacity;
    
    private readonly List<Passenger> passengers = new();
    private readonly List<Transform> activeSeatPositions = new();
    private readonly List<Renderer> seatIndicators = new();
    [SerializeField] private GameObject busModel;
    private Material material;
    [SerializeField]
    private Transform[] seatPositions;
    private Transform seatIndicatorRoot;
    private Vector3 busModelBaseScale;
    private Vector3[] seatBaseLocalPositions;

    private void Awake()
    {
        busModelBaseScale = busModel.transform.localScale;
        seatBaseLocalPositions = new Vector3[seatPositions.Length];
        for (int i = 0; i < seatPositions.Length; i++)
        {
            seatBaseLocalPositions[i] = seatPositions[i].localPosition;
        }
    }

    public void Initialize(Color color, int capacity)
    {
        Color = color;
        Capacity = Mathf.Clamp(capacity, 1, seatPositions.Length);
        passengers.Clear();
        material = busModel.GetComponent<Renderer>().material;
        material.color = color;

        ConfigureLayout();
        RebuildSeatIndicators();
        RefreshSeatIndicators();
    }

    public void AddPassenger(Passenger passenger)
    {
        if (IsFull) return;

        int seatIndex = passengers.Count;
        Transform seatTransform = activeSeatPositions[seatIndex];
        
        StartCoroutine(MovePassengerToBus(passenger, seatTransform.position));
        
        passengers.Add(passenger);
        RefreshSeatIndicators();
    }

    private void ConfigureLayout()
    {
        activeSeatPositions.Clear();
        busModel.transform.localScale = busModelBaseScale;

        for (int i = 0; i < seatPositions.Length; i++)
        {
            seatPositions[i].localPosition = seatBaseLocalPositions[i];
        }

        if (Capacity == 1)
        {
            int centerIndex = Mathf.Min(1, seatPositions.Length - 1);
            activeSeatPositions.Add(seatPositions[centerIndex]);
            return;
        }

        if (Capacity == 2 && seatPositions.Length >= 3)
        {
            busModel.transform.localScale = new Vector3(
                busModelBaseScale.x * TwoSeatWidthScale,
                busModelBaseScale.y,
                busModelBaseScale.z * TwoSeatDepthScale);

            seatPositions[0].localPosition = new Vector3(-0.68f, seatBaseLocalPositions[0].y, seatBaseLocalPositions[0].z);
            seatPositions[2].localPosition = new Vector3(0.68f, seatBaseLocalPositions[2].y, seatBaseLocalPositions[2].z);

            activeSeatPositions.Add(seatPositions[0]);
            activeSeatPositions.Add(seatPositions[2]);
            return;
        }

        for (int i = 0; i < Capacity; i++)
        {
            activeSeatPositions.Add(seatPositions[i]);
        }
    }

    private void RebuildSeatIndicators()
    {
        if (seatIndicatorRoot != null)
        {
            Destroy(seatIndicatorRoot.gameObject);
        }

        seatIndicators.Clear();
        seatIndicatorRoot = new GameObject("SeatIndicators").transform;
        seatIndicatorRoot.SetParent(transform, false);

        float indicatorWidth = Capacity == 2 ? 0.46f : 0.32f;
        float indicatorDepth = Capacity == 2 ? 0.26f : 0.22f;

        for (int i = 0; i < activeSeatPositions.Count; i++)
        {
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.name = $"SeatIndicator-{i + 1}";
            indicator.transform.SetParent(seatIndicatorRoot, false);

            Vector3 seatPosition = activeSeatPositions[i].localPosition;
            indicator.transform.localPosition = new Vector3(seatPosition.x, SeatIndicatorY, SeatIndicatorZ);
            indicator.transform.localScale = new Vector3(indicatorWidth, 0.06f, indicatorDepth);

            Collider collider = indicator.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = indicator.GetComponent<Renderer>();
            renderer.material.color = GetEmptySeatColor();
            seatIndicators.Add(renderer);
        }
    }

    private void RefreshSeatIndicators()
    {
        Color emptySeatColor = GetEmptySeatColor();
        Color occupiedSeatColor = GetOccupiedSeatColor();

        for (int i = 0; i < seatIndicators.Count; i++)
        {
            seatIndicators[i].material.color = i < passengers.Count ? occupiedSeatColor : emptySeatColor;
        }
    }

    private Color GetEmptySeatColor()
    {
        float brightness = Color.r * 0.299f + Color.g * 0.587f + Color.b * 0.114f;
        return brightness > 0.65f ? new Color(0.13f, 0.13f, 0.16f) : new Color(0.95f, 0.95f, 0.92f);
    }

    private Color GetOccupiedSeatColor()
    {
        Color contrast = GetEmptySeatColor();
        return Color.Lerp(contrast, Color, 0.55f);
    }
    
    
    private System.Collections.IEnumerator MovePassengerToBus(Passenger passenger, Vector3 targetPos)
    {
        float moveTime = 0.2f;
        float elapsed = 0;
        Vector3 startPos = passenger.transform.position;

        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;
            passenger.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        passenger.transform.SetParent(transform);
        passenger.transform.position = targetPos;
        
       
        
    }
}
