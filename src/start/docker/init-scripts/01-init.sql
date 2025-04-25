CREATE TABLE IF NOT EXISTS "TodoItems" (
    "Id" SERIAL PRIMARY KEY,
    "Title" VARCHAR(255) NOT NULL,
    "IsComplete" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CompletedAt" TIMESTAMP WITH TIME ZONE NULL
);


-- 接続の許可設定
ALTER SYSTEM SET listen_addresses TO '*';

-- データベースユーザーに適切な権限を付与
GRANT ALL PRIVILEGES ON TABLE "TodoItems" TO postgres;
GRANT USAGE, SELECT ON SEQUENCE "TodoItems_Id_seq" TO postgres;