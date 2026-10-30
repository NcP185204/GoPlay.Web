namespace GoPlay.Web.Enums;

// Loại sân. Lưu DB dạng chuỗi qua HasConversion<string>() (tương đương @Enumerated(STRING)).
public enum SportType
{
    Football,
    Basketball,
    Tennis,
    Badminton,
    Volleyball
}