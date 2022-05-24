using Umbraco.Core.Persistence.DatabaseAnnotations;
using System;
using NPoco;

namespace DCHMediaPicker.Data.Models
{
    [TableName("DCHTrackedMediaItem")]
    [PrimaryKey("Id", AutoIncrement = true)]
    [ExplicitColumns]
    public class TrackedMediaItem
    {
        [Column("Id")]
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int Id { get; set; }

        [Column("UserId")]
        public int UserId { get; set; }

        [Column("NodeId")]
        public int NodeId { get; set; }

        [Column("Title")]
        public string Title { get; set; }

        [Column("Url")]
        public string Url { get; set; }

        [Column("Thumbnail")]
        public string Thumbnail { get; set; }

        [Column("MediaId")]
        public int MediaId { get; set; }

        [Column("FileName")]
        public string FileName { get; set; }

        [Column("Expiry")]
        public DateTime? Expiry { get; set; }

        [Column("ReminderSent")]
        public bool ReminderSent { get; set; }

        [Column("ProjectId")]
        public int ProjectId { get; set; }
    }
}