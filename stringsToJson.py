import os
import json
from openpyxl import load_workbook

WORKING_DIR = os.path.dirname(os.path.realpath(__file__))
IN_FILE = os.path.join(WORKING_DIR, "Strings.xlsx")
OUT_DIR = os.path.join(WORKING_DIR, "TheOtherRoles", "Resources", "Translations")

# Excel column header -> (language code, SupportedLangs int id)
# Language codes use BCP 47 / IETF tags for Weblate compatibility
LANGUAGE_MAP = {
    "English":   ("en",       0),
    "Latam":     ("es_419",   1),
    "Brazilian": ("pt_BR",    2),
    "Portuguese": ("pt",      3),
    "Korean":    ("ko",       4),
    "Russian":   ("ru",       5),
    "Dutch":     ("nl",       6),
    "Filipino":  ("fil",      7),
    "French":    ("fr",       8),
    "German":    ("de",       9),
    "Italian":   ("it",      10),
    "Japanese":  ("ja",      11),
    "Spanish":   ("es",      12),
    "SChinese":  ("zh_Hans", 13),
    "TChinese":  ("zh_Hant", 14),
    "Irish":     ("ga",      15),
}

def stringToJson(in_files):
    # lang_code -> { "Category,Id": "text" }
    lang_data = {}

    for filename in in_files:
        if not os.path.isfile(filename):
            print(f"File not found: {filename}")
            continue

        wb = load_workbook(filename, read_only=True)

        for s in wb:
            # Read header row to get language column names
            headers = []
            rows = s.iter_rows(min_col=1, min_row=1, max_col=18, max_row=1)
            for row in rows:
                for cell in row[2:]:
                    if cell.value:
                        headers.append(cell.value)

            # Read all data rows
            rows = s.iter_rows(min_col=1, min_row=1, max_col=18, max_row=None)
            for row in rows:
                key = f"{row[0].value},{row[1].value}"
                if key == "Category,Id":
                    continue
                if not row[0].value or row[1].value is None:
                    continue

                for i, cell in enumerate(row[2:]):
                    if cell.value and i < len(headers):
                        header = headers[i]
                        if header in LANGUAGE_MAP:
                            lang_code, _ = LANGUAGE_MAP[header]
                            if lang_code not in lang_data:
                                lang_data[lang_code] = {}
                            text = str(cell.value).replace("\r", "").replace("_x000D_", "").replace("\\n", "\n")
                            lang_data[lang_code][key] = text

        wb.close()

    # Write one JSON file per language
    os.makedirs(OUT_DIR, exist_ok=True)
    for lang_code, data in sorted(lang_data.items()):
        out_file = os.path.join(OUT_DIR, f"{lang_code}.json")
        with open(out_file, "w", newline="\n", encoding="utf-8") as f:
            json.dump(data, f, indent=4, ensure_ascii=False)
        print(f"Written: {out_file} ({len(data)} entries)")

    print(f"\nTotal: {len(lang_data)} language files generated")

if __name__ == "__main__":
    in_files = [
        os.path.join(WORKING_DIR, "Strings.xlsx")
    ]
    stringToJson(in_files)
