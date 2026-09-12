import openpyxl

wb = openpyxl.load_workbook('Strings.xlsx')
ws = wb['Text']

# Collect all role entries: {roleId: {category: (row_data)}}
role_entries = {}
header_row = None
normal_rows = []

for row in ws.iter_rows(min_row=1, max_col=18):
    cat = row[0].value
    if row[0].value == 'Category' and row[1].value == 'Id':
        header_row = [cell.value for cell in row]
        continue

    if cat and cat.startswith('Role-') and row[1].value is not None:
        role_id = row[1].value
        if role_id not in role_entries:
            role_entries[role_id] = {}
        role_entries[role_id][cat] = [cell.value for cell in row]
    else:
        normal_rows.append([cell.value for cell in row])

# Clear sheet
for row in ws.iter_rows(min_row=1, max_row=ws.max_row, max_col=18):
    for cell in row:
        cell.value = None

# Write header
for col, val in enumerate(header_row, 1):
    ws.cell(row=1, column=col, value=val)

# Write normal rows first
row_num = 2
for row_data in normal_rows:
    for col, val in enumerate(row_data, 1):
        ws.cell(row=row_num, column=col, value=val)
    row_num += 1

# Write role entries grouped by roleId
for role_id in sorted(role_entries.keys()):
    for cat in ['Role-Name', 'Role-IntroDesc', 'Role-ShortDesc', 'Role-Desc']:
        if cat in role_entries[role_id]:
            for col, val in enumerate(role_entries[role_id][cat], 1):
                ws.cell(row=row_num, column=col, value=val)
            row_num += 1

wb.save('Strings.xlsx')
print(f'Done. Total rows: {ws.max_row}')
print(f'Normal rows: {len(normal_rows)}')
print(f'Role entries: {len(role_entries) * 4} ({len(role_entries)} roles x 4 categories)')
