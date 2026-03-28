using Firebase.Firestore;

[FirestoreData]
public class UrlData
{
    [FirestoreProperty]
    public string gitURL { get; set; }

    [FirestoreProperty]
    public string highURL { get; set; }


}