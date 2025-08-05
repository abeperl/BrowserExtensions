F1::  ; Press F1 to trigger
Send ^c  ; Copy selected text
Run "winword.exe C:\path\to\your\template.dotx"  ; Open Word template
Sleep 2000  ; Wait for Word to load
Send ^v  ; Paste the text
return