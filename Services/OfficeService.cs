using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Presentation;
using MSOfficeAuthors.Models;
using Author = DocumentFormat.OpenXml.Spreadsheet.Author;
using Comment = DocumentFormat.OpenXml.Wordprocessing.Comment;
using CommentAuthor = DocumentFormat.OpenXml.Presentation.CommentAuthor;
using Microsoft.Extensions.Logging;

namespace MSOfficeAuthors.Services
{
    public class OfficeService(ILogger<OfficeService> logger) : IOfficeService
    {
        private readonly ILogger<OfficeService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public void Dispose()
        {
            // No unmanaged resources to dispose, but implementing IDisposable allows
            // DI container to manage lifecycle if resources are added later.
            GC.SuppressFinalize(this);
        }

        public async Task<List<AuthorEntry>> GetAuthorsAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                List<AuthorEntry> authors = [];
                string extension = Path.GetExtension(filePath).ToLower();

                try
                {
                    using (var doc = OpenPackage(filePath, extension, false))
                    {
                        if (doc != null)
                        {
                            authors.AddRange(GetCoreProperties(doc, filePath));
                            authors.AddRange(GetDocumentAuthors(doc, filePath));
                        }
                    }
                }
                catch (FileNotFoundException ex)
                {
                    _logger.LogWarning(ex, "File not found during author retrieval: {FilePath}", filePath);
                    throw;
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogError(ex, "Unauthorized access during author retrieval: {FilePath}", filePath);
                    throw;
                }
                catch (InvalidDataException ex)
                {
                    _logger.LogError(ex, "Invalid or corrupted office document: {FilePath}", filePath);
                    throw;
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "IO error during author retrieval: {FilePath}", filePath);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error retrieving authors from {FilePath}", filePath);
                    throw;
                }

                return authors;
            });
        }

        private List<AuthorEntry> GetDocumentAuthors(OpenXmlPackage doc, string filePath)
        {
            return doc switch
            {
                WordprocessingDocument wordDoc => GetWordAuthors(wordDoc, filePath),
                SpreadsheetDocument excelDoc => GetExcelAuthors(excelDoc, filePath),
                PresentationDocument pptDoc => GetPptAuthors(pptDoc, filePath),
                _ => []
            };
        }

        private OpenXmlPackage? OpenPackage(string filePath, string extension, bool isEditable)
        {
            return extension switch
            {
                ".docx" => WordprocessingDocument.Open(filePath, isEditable),
                ".xlsx" => SpreadsheetDocument.Open(filePath, isEditable),
                ".pptx" => PresentationDocument.Open(filePath, isEditable),
                _ => null
            };
        }

        private List<AuthorEntry> GetCoreProperties(OpenXmlPackage package, string filePath)
        {
            var props = package.PackageProperties;
            if (props == null) return [];

            return GetAuthors(filePath, EntityType.Property, [props.Creator, props.LastModifiedBy]);
        }

        private List<AuthorEntry> GetAuthors(string filePath, EntityType type, IEnumerable<string?> authors)
        {
            return authors
                .Where(a => !string.IsNullOrEmpty(a))
                .Distinct()
                .Select(author => new AuthorEntry 
                { 
                    FilePath = filePath, 
                    Type = type, 
                    OriginalAuthorName = author!, 
                    NewAuthorName = author! 
                })
                .ToList();
        }

        private List<AuthorEntry> GetWordAuthors(WordprocessingDocument doc, string filePath)
        {
            List<AuthorEntry> result = [];
            if (doc.MainDocumentPart?.Document == null) return result;

            // Revisions
            var body = doc.MainDocumentPart.Document.Body;
            if (body != null)
            {
                var authorsInRevisions = body.Descendants<TrackChangeType>().Select(c => c.Author?.Value)
                    .Concat(body.Descendants<RunTrackChangeType>().Select(c => c.Author?.Value));
                
                result.AddRange(GetAuthors(filePath, EntityType.Revision, authorsInRevisions));
            }

            // Comments
            var commentsPart = doc.MainDocumentPart.WordprocessingCommentsPart;
            if (commentsPart?.Comments != null)
            {
                var commentAuthors = commentsPart.Comments.Elements<Comment>().Select(c => c.Author?.Value);
                result.AddRange(GetAuthors(filePath, EntityType.Comment, commentAuthors));
            }
            return result;
        }

        private List<AuthorEntry> GetExcelAuthors(SpreadsheetDocument doc, string filePath)
        {
            List<AuthorEntry> result = [];
            if (doc.WorkbookPart == null) return result;

            // Comments in Excel (Legacy and modern)
            foreach (var worksheetPart in doc.WorkbookPart.WorksheetParts)
            {
                var commentsPart = worksheetPart.WorksheetCommentsPart;
                if (commentsPart != null && commentsPart.Comments != null && commentsPart.Comments.Authors != null)
                {
                    var authors = commentsPart.Comments.Authors.Elements<Author>().Select(a => a.Text.ToString());
                    result.AddRange(GetAuthors(filePath, EntityType.Comment, authors));
                }
            }
            return result;
        }

        private List<AuthorEntry> GetPptAuthors(PresentationDocument doc, string filePath)
        {

            List<AuthorEntry> result = [];
            if (doc.PresentationPart == null) return result;
            var commentAuthorsPart = doc.PresentationPart.CommentAuthorsPart;
            if (commentAuthorsPart?.CommentAuthorList != null)
            {
                var authors = commentAuthorsPart.CommentAuthorList.Elements<CommentAuthor>().Select(a => a.Name?.Value);
                result.AddRange(GetAuthors(filePath, EntityType.Comment, authors));
            }
            return result;
        }


        public async Task SaveChangesAsync(IEnumerable<AuthorEntry> entries)
        {
            // Materialize all data before entering background thread to avoid cross-thread collection access
            var groupedByFile = entries
                .GroupBy(e => e.FilePath)
                .Select(g => new { FilePath = g.Key, Entries = g.ToList() })
                .ToList();

            await Task.Run(() =>
            {
                foreach (var group in groupedByFile)
                {
                    string filePath = group.FilePath;
                    string extension = Path.GetExtension(filePath).ToLower();
                    var entryList = group.Entries;

                    _logger.LogInformation("Saving {Count} entries to {FilePath}", entryList.Count, filePath);
                    foreach (var e in entryList)
                    {
                        _logger.LogDebug("  Entry: Type={Type}, Original='{Original}', New='{New}'", e.Type, e.OriginalAuthorName, e.NewAuthorName);
                    }

                    try
                    {
                        using (var doc = OpenPackage(filePath, extension, true))
                        {
                            if (doc != null)
                            {
                                UpdateCoreProps(doc, entryList);
                                UpdateDocumentAuthors(doc, entryList);
                                doc.Save();
                                _logger.LogInformation("File saved successfully: {FilePath}", filePath);
                            }
                            else
                            {
                                _logger.LogWarning("Unsupported file format, skipping: {FilePath}", filePath);
                            }
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogError(ex, "Access denied when saving changes to {FilePath}", filePath);
                        throw;
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "IO error when saving changes to {FilePath}", filePath);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error when saving changes to {FilePath}", filePath);
                        throw new IOException($"Unexpected error updating file {filePath}: {ex.Message}", ex);
                    }
                }
            });
        }

        private void UpdateDocumentAuthors(OpenXmlPackage doc, List<AuthorEntry> entries)
        {
            switch (doc)
            {
                case WordprocessingDocument wordDoc:
                    UpdateWordComments(wordDoc, entries);
                    break;
                case SpreadsheetDocument excelDoc:
                    UpdateExcelComments(excelDoc, entries);
                    break;
                case PresentationDocument pptDoc:
                    UpdatePptComments(pptDoc, entries);
                    break;
            }
        }

        private void UpdateWordComments(WordprocessingDocument doc, IEnumerable<AuthorEntry> entries)
        {
            var commentsPart = doc.MainDocumentPart?.WordprocessingCommentsPart;
            if (commentsPart?.Comments != null)
            {
                UpdateAuthors(commentsPart.Comments.Elements<Comment>(), c => c.Author, entries, EntityType.Comment);
                commentsPart.Comments.Save();
            }

            var body = doc.MainDocumentPart?.Document?.Body;
            if (body != null && doc.MainDocumentPart?.Document != null)
            {
                UpdateAuthors(body.Descendants<TrackChangeType>(), c => c.Author, entries, EntityType.Revision);
                UpdateAuthors(body.Descendants<RunTrackChangeType>(), c => c.Author, entries, EntityType.Revision);
                doc.MainDocumentPart.Document.Save();
            }
        }

        private void UpdateExcelComments(SpreadsheetDocument doc, IEnumerable<AuthorEntry> entries)
        {
            var workbookPart = doc.WorkbookPart;
            if (workbookPart != null)
            {
                foreach (var sheet in workbookPart.WorksheetParts)
                {
                    var commentsPart = sheet.WorksheetCommentsPart;
                    if (commentsPart?.Comments?.Authors != null)
                    {
                        UpdateAuthors(commentsPart.Comments.Authors.Elements<Author>(), a => a.Text, (a, v) => a.Text = v, entries, EntityType.Comment);
                        commentsPart.Comments.Save();
                    }
                }
            }
        }

        private void UpdatePptComments(PresentationDocument doc, IEnumerable<AuthorEntry> entries)
        {
            var authorListPart = doc.PresentationPart?.CommentAuthorsPart;
            if (authorListPart?.CommentAuthorList != null)
            {
                UpdateAuthors(authorListPart.CommentAuthorList.Elements<CommentAuthor>(), a => a.Name, entries, EntityType.Comment);
                authorListPart.CommentAuthorList.Save();
            }
        }

        private void UpdateAuthors<T>(IEnumerable<T> elements, Func<T, DocumentFormat.OpenXml.StringValue?> authorSelector, IEnumerable<AuthorEntry> entries, EntityType type)
        {
            var lookup = entries
                .Where(e => e.Type == type)
                .GroupBy(e => e.OriginalAuthorName)
                .ToDictionary(g => g.Key, g => g.First().NewAuthorName);

            foreach (var element in elements)
            {
                var authorValue = authorSelector(element);
                if (authorValue?.Value != null && lookup.TryGetValue(authorValue.Value, out var newName))
                {
                    authorValue.Value = newName;
                }
            }
        }

        private void UpdateAuthors<T>(IEnumerable<T> elements, Func<T, string?> authorGetter, Action<T, string> authorSetter, IEnumerable<AuthorEntry> entries, EntityType type)
        {
            var lookup = entries
                .Where(e => e.Type == type)
                .GroupBy(e => e.OriginalAuthorName)
                .ToDictionary(g => g.Key, g => g.First().NewAuthorName);

            foreach (var element in elements)
            {
                var authorName = authorGetter(element);
                if (authorName != null && lookup.TryGetValue(authorName, out var newName))
                {
                    authorSetter(element, newName);
                }
            }
        }

        private void UpdateCoreProps(OpenXmlPackage package, IEnumerable<AuthorEntry> entries)
        {
            if (package.PackageProperties == null) return;

            var coreProps = entries.Where(e => e.Type == EntityType.Property);
            foreach (var prop in coreProps)
            {
                if (package.PackageProperties.Creator == prop.OriginalAuthorName)
                    package.PackageProperties.Creator = prop.NewAuthorName;
                if (package.PackageProperties.LastModifiedBy == prop.OriginalAuthorName)
                    package.PackageProperties.LastModifiedBy = prop.NewAuthorName;
            }
        }
    }
}

