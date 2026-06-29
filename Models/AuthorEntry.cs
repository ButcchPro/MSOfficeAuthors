using CommunityToolkit.Mvvm.ComponentModel;

namespace MSOfficeAuthors.Models
{
    public enum EntityType
    {
        Property,
        Revision,
        Comment
    }

    public partial class AuthorEntry : ObservableObject
    {
        [ObservableProperty]
        private string _newAuthorName = string.Empty;
        
        public required string FilePath { get; set; }
        public string FileName => System.IO.Path.GetFileName(FilePath) ?? string.Empty;
        public required EntityType Type { get; set; }
        public required string OriginalAuthorName { get; set; }
        
        public object? UnderlyingObject { get; set; }
    }
}
