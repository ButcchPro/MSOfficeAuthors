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
        
        public string FilePath { get; set; } = string.Empty;
        public string FileName => System.IO.Path.GetFileName(FilePath) ?? string.Empty;
        public EntityType Type { get; set; }
        public string OriginalAuthorName { get; set; } = string.Empty;
        
        public object? UnderlyingObject { get; set; }
    }
}
