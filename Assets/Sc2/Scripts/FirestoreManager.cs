using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Extensions;

public class FirestoreManager : MonoBehaviour
{
    [SerializeField] Button countButton;
    [SerializeField] TextMeshProUGUI number;

    FirebaseFirestore db;
    ListenerRegistration listenerRegistration;

   [SerializeField] private string username;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance; // Connects to Firebase database

        countButton.onClick.AddListener(OnClick);

        listenerRegistration = db.Collection("counters")
            .Document("counter")
            .Listen(snapshot =>
            {
                Counter counter = snapshot.ConvertTo<Counter>();
                number.text = counter.Count.ToString();
            });
    }

    void OnDestroy()
    {
        listenerRegistration.Stop();
    }

    void OnClick()
    {
        int oldCount = int.Parse(number.text);

        Counter counter = new Counter
        {
            Count = oldCount + 1,
            UpdatedBy = username
        };

        db.Collection("counters").Document("counter")
            .SetAsync(counter);
    }
}