### How it work?
This script automates building the data behind a searchable Tabler icon picker. It scans the Tabler outline icons that are installed in the project, checks which icons are missing search keywords, and fills in the gaps automatically. For any missing icon, it asks an AI service to generate a small set of human-friendly keywords that someone would realistically type when searching for that icon, then saves those keywords so the process is repeatable. Once the keyword list is complete, the script generates a final icons file that combines each icon’s name, CSS class, search terms, and SVG content into one clean, sorted JSON file. The end result is a consistently updated, ready-to-use icon catalog that powers the icon picker without any manual maintenance.

### Instruction:
1. Navigate to Rock/Dev Tools/tabler-icon-updater.  
2. Open the project in VS Code.
3. Open the package.json
4. Increment the @tabler/icons-webfont version to the desired version.
    Note: Only version increments are supported—you cannot downgrade. If you mess up, just discard your Git 6. changes and retry.
5. Run npm install to install the updated package.
6. In the terminal, set the OpenAI API key: $env:OPENAI_KEY=""
Note: You can use the triumph OpenAI Key in RDM 

7. Run npm run generateIcon.
8. The tabler-icons.json file will be regenerated with the new icons.
9. Copy its contents and paste them into:
    RockWeb/Assets/Libraries/tabler-icons.json
10. Restart Rock and clear cache then make sure it applies.
11. Done.
