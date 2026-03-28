using Firebase.Firestore;

[FirestoreData]
public struct Retrieve
{
    [FirestoreProperty]
    public int Age { get; set; }

    [FirestoreProperty]
    public string username { get; set; }

/*
    [FirestoreProperty]
    public string gitURL { get; set; }

    [FirestoreProperty]
    public string name { get; set; }*/
}