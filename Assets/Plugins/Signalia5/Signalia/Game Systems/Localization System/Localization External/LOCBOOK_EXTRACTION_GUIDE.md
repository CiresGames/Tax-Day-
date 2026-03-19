# Locbook Extraction Guide

## Overview

The Locbook Extraction system provides a straightforward way to extract non-localized strings from your game and prepare them for localization. This tool is designed to be efficient, quick, and accurate.

## How It Works

The extraction system uses the `ILocbookExtraction` interface. Any MonoBehaviour or ScriptableObject that implements this interface can be scanned and have its text automatically extracted into a LocBook.

### The Extraction Flow

1. **Implement Interface**: Add `ILocbookExtraction` to your classes that contain localizable text
2. **Define Extraction Logic**: Implement `GetExtractionData()` to specify what text to extract
3. **Run Extractor**: Use `Tools > Signalia > Game Systems > Localization > Extract Locbook`
4. **Generate LocBook**: The tool creates a LocBook asset and .locbook file
5. **Update Code**: Replace `text = value` with `.SetLocalizedText(value)`
6. **Localize**: Use Lingramia to add translations to the .locbook file

## Quick Start

### Step 1: Implement ILocbookExtraction

For a **ScriptableObject**:

```csharp
using AHAKuo.Signalia.GameSystems.Localization.External;

public class MyDialogue : ScriptableObject, ILocbookExtraction
{
    public string characterName = "Hero";
    public string dialogueText = "Hello, world!";
    
    public ExtractionData GetExtractionData()
    {
        var data = new ExtractionData();
        var page = new ExtractionPage
        {
            pageName = $"Dialogue: {name}",
            about = "Dialogue content",
            fields = new List<ExtractionPageField>()
        };
        
        page.fields.Add(new ExtractionPageField
        {
            originalValue = characterName,
            key = "" // Leave empty to auto-generate
        });
        
        page.fields.Add(new ExtractionPageField
        {
            originalValue = dialogueText,
            key = "" // Or specify: key = "dialogue_hero_greeting"
        });
        
        data.pages.Add(page);
        return data;
    }
}
```

For a **MonoBehaviour**:

```csharp
using AHAKuo.Signalia.GameSystems.Localization.External;

public class UIController : MonoBehaviour, ILocbookExtraction
{
    public string titleText = "Main Menu";
    public List<string> buttonLabels = new List<string> { "Play", "Options", "Quit" };
    
    public ExtractionData GetExtractionData()
    {
        var data = new ExtractionData();
        var page = new ExtractionPage
        {
            pageName = $"UI: {gameObject.name}",
            about = "UI text elements",
            fields = new List<ExtractionPageField>()
        };
        
        page.fields.Add(new ExtractionPageField(titleText));
        
        foreach (var label in buttonLabels)
        {
            page.fields.Add(new ExtractionPageField(label));
        }
        
        data.pages.Add(page);
        return data;
    }
}
```

### Step 2: Place Assets in Resources

For **ScriptableObjects** to be discovered:
- Place them in any folder named `Resources` (e.g., `Assets/Resources/` or `Assets/MyGame/Resources/`)
- The extractor will recursively scan all Resources folders

For **MonoBehaviours**:
- Add them to GameObjects in your scenes
- Make sure the scenes are **open** when running the extractor

### Step 3: Run the Extractor

1. Open the menu: `Tools > Signalia > Game Systems > Localization > Extract Locbook`
2. Read the instructions in the window
3. Click **"Start Extraction"**
4. Choose where to save the LocBook asset
5. Review the extraction results

### Step 4: Update Your Code

After extraction, update your code to use the localization system:

**Before:**
```csharp
tmpText.text = titleText;
```

**After (with Hybrid Key mode enabled):**
```csharp
tmpText.SetLocalizedText(titleText);
```

**After (with keys):**
```csharp
tmpText.SetLocalizedText("ui_mainmenu_title");
```

### Step 5: Localize in Lingramia

1. Find the generated `.locbook` file (same location as the LocBook asset)
2. Open it with Lingramia (or click "Open in Lingramia" button on the LocBook asset)
3. Add translations for your supported languages
4. Save in Lingramia
5. Return to Unity and click "Update Asset from .locbook File"

## Key Concepts

### Pages

Pages are logical groupings of localization entries. You can organize your text however makes sense:
- One page per asset
- Multiple pages per asset (e.g., separate pages for UI, dialogue, errors)
- One page per feature/system

### Fields

Fields are individual text entries that need localization. Each field has:
- **originalValue** (required): The source text
- **key** (optional): A unique identifier for code lookup
- **variants** (optional): Pre-existing translations (rarely needed)

### Keys vs Hybrid Key Mode

**With Keys:**
- Set `key` on each field: `key = "quest_start_dialogue"`
- Use in code: `tmpText.SetLocalizedText("quest_start_dialogue")`
- Pros: Stable, won't break if text changes
- Cons: Requires manual key management

**With Hybrid Key Mode:**
- Leave `key` empty: `key = ""`
- Use in code: `tmpText.SetLocalizedText("Hello, world!")`
- Pros: Quick, no key management needed
- Cons: Breaks if you change the original text

**Recommendation**: Use Hybrid Key mode during early development, switch to keys when text stabilizes.

## Examples

Check the `Examples` folder for complete working examples:

- **ExampleDialogueAsset.cs**: Simple dialogue system extraction
- **ExampleUIController.cs**: UI text extraction from MonoBehaviour
- **ExampleQuestData.cs**: Complex multi-page extraction

## Advanced Usage

### Multiple Pages per Asset

You can return multiple pages to organize your content:

```csharp
public ExtractionData GetExtractionData()
{
    var data = new ExtractionData();
    
    // Page 1: Titles
    data.pages.Add(new ExtractionPage
    {
        pageName = "Quest Titles",
        about = "All quest title strings"
    });
    
    // Page 2: Descriptions
    data.pages.Add(new ExtractionPage
    {
        pageName = "Quest Descriptions",
        about = "All quest description strings"
    });
    
    return data;
}
```

### Pre-existing Translations

If you already have translations, include them:

```csharp
var field = new ExtractionPageField
{
    originalValue = "Hello",
    key = "greeting_hello",
    variants = new List<ExtractionLanguageVariant>
    {
        new ExtractionLanguageVariant("es", "Hola"),
        new ExtractionLanguageVariant("fr", "Bonjour")
    }
};
```

### Custom Page IDs

Control page IDs for better organization:

```csharp
var page = new ExtractionPage
{
    pageId = "ui_mainmenu", // Custom ID
    pageName = "Main Menu UI",
    about = "Main menu interface text"
};
```

## Troubleshooting

### "No objects implementing ILocbookExtraction were found"

**Solutions:**
- Ensure your classes implement the interface correctly
- For ScriptableObjects: Place them in Resources folders
- For MonoBehaviours: Make sure their scenes are open
- Check the console for any extraction errors

### "Failed to extract from [object]"

**Possible causes:**
- `GetExtractionData()` is throwing an exception
- Check the console for the specific error message
- Ensure `originalValue` is not null/empty for fields

### Keys are not unique

The system auto-generates unique keys by appending numbers. If you want specific keys:
- Set the `key` field manually on each `ExtractionPageField`
- Use a consistent naming convention (e.g., `category_object_field`)

## Integration with Signalia Config

After creating your LocBook:

1. Open your Signalia config asset
2. Find the Localization System section
3. Add the LocBook to the LocBooks array (or use "Load in Config" button)
4. Set your default language
5. Enable Hybrid Key mode if desired

## Best Practices

1. **Organize by Feature**: Create separate assets for different game systems
2. **Descriptive Page Names**: Use clear names like "UI: Main Menu" or "Quest: Chapter 1"
3. **Consistent Keys**: If using keys, follow a naming convention
4. **Extract Early**: Run extraction early in development to identify all text
5. **Document Context**: Use the `about` field to explain what each page contains
6. **Version Control**: Commit both .asset and .locbook files

## Workflow Recommendations

### For Rapid Prototyping
1. Use Hybrid Key mode
2. Don't set keys (leave them empty)
3. Use original values directly: `SetLocalizedText("Click Me")`
4. Quick and flexible

### For Production
1. Disable Hybrid Key mode
2. Use specific keys: `SetLocalizedText("ui_button_submit")`
3. More stable across text changes
4. Better for large teams

## Additional Resources

- See `LOCALIZATION_REFACTOR_SUMMARY.md` for the overall localization workflow
- Check the `Examples` folder for implementation examples
- Refer to the Lingramia documentation for translation features

---

**Task ID**: 86evbpgxf  
**Feature**: Localization Extractor Tools
