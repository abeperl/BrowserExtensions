# Try updating the URL parameter from ?id= to ?PicklistID=
$db = "api_config.db"
$query = @"
UPDATE SubAction 
SET Configuration = json_set(
    Configuration, 
    '$.Endpoint', 
    replace(json_extract(Configuration, '$.Endpoint'), '?id=', '?PicklistID=')
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2) 
  AND ActionNumber = 1
  AND json_extract(Configuration, '$.Endpoint') LIKE '%?id=%';
"@

Write-Host "Updating URL parameter from ?id= to ?PicklistID="
sqlite3 $db $query

# Verify the change
$verify = "SELECT json_extract(Configuration, '$.Endpoint') FROM SubAction WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2) AND ActionNumber = 1;"
Write-Host "`nNew endpoint:"
sqlite3 $db $verify
