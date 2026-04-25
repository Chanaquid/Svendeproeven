using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class BackfillLastMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            UPDATE dc
            SET dc.LastMessageId = lm.Id
            FROM DirectConversations dc
            INNER JOIN (
                SELECT 
                    ConversationId,
                    Id,
                    ROW_NUMBER() OVER (PARTITION BY ConversationId ORDER BY SentAt DESC) as rn
                FROM DirectMessages
            ) lm ON lm.ConversationId = dc.Id AND lm.rn = 1
            WHERE dc.LastMessageId IS NULL
        ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE DirectConversations SET LastMessageId = NULL");

        }
    }
}
