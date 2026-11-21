from docx import Document
from docx.shared import Pt, Inches, Cm
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.enum.text import WD_BREAK
from docx.enum.style import WD_STYLE_TYPE
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
DOCS=ROOT/'docs'
BLACK='000000'

def set_color(run):
    run.font.color.rgb = __import__('docx').shared.RGBColor(0,0,0)

def base_doc():
    d=Document()
    sec=d.sections[0]
    sec.page_width=Cm(21); sec.page_height=Cm(29.7)
    sec.top_margin=Cm(2.0); sec.bottom_margin=Cm(1.8)
    sec.left_margin=Cm(2.4); sec.right_margin=Cm(1.8)
    sec.header_distance=Cm(1.0); sec.footer_distance=Cm(1.0)
    n=d.styles['Normal'];n.font.name='Times New Roman';n.font.size=Pt(13.5); n.font.color.rgb=__import__('docx').shared.RGBColor(0,0,0)
    n.paragraph_format.space_after=Pt(4); n.paragraph_format.line_spacing=1.25
    for i in range(1,4):
        st=d.styles[f'Heading {i}'];st.font.name='Times New Roman';st.font.color.rgb=__import__('docx').shared.RGBColor(0,0,0)
        st.font.size=Pt(14 if i>1 else 15);st.font.bold=True
        st.paragraph_format.space_before=Pt(12);st.paragraph_format.space_after=Pt(6)
        st.paragraph_format.keep_with_next=True
    style=d.styles.add_style('CodeLab',WD_STYLE_TYPE.PARAGRAPH)
    style.font.name='Consolas';style.font.size=Pt(9);style.font.color.rgb=__import__('docx').shared.RGBColor(0,0,0)
    style.paragraph_format.line_spacing=1.0;style.paragraph_format.space_before=Pt(0);style.paragraph_format.space_after=Pt(0)
    f=sec.footer.paragraphs[0]; f.alignment=WD_ALIGN_PARAGRAPH.RIGHT
    f.add_run('Сторінка ')
    field=OxmlElement('w:fldSimple'); field.set(qn('w:instr'),'PAGE');f._p.append(field)
    return d

def text(d,s,align=None):
    p=d.add_paragraph(s)
    if align is not None:p.alignment=align
    return p

def h(d,n,s): d.add_heading(f'{n} {s}', level=1)
def cap(d,t):
    p=d.add_paragraph(t);p.alignment=WD_ALIGN_PARAGRAPH.CENTER
    for r in p.runs:r.italic=True;r.font.size=Pt(11);set_color(r)

def table(d,header,rows,widths=None,font=10.2):
    t=d.add_table(rows=1, cols=len(header));t.style='Table Grid';t.alignment=WD_TABLE_ALIGNMENT.CENTER
    for i,hdr in enumerate(header):t.rows[0].cells[i].text=hdr
    for row in rows:
        r=t.add_row()
        for i,v in enumerate(row):r.cells[i].text=str(v)
    for rid,row in enumerate(t.rows):
        for j,cell in enumerate(row.cells):
            cell.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
            if widths:cell.width=Cm(widths[j])
            for p in cell.paragraphs:
                p.paragraph_format.line_spacing=1.0;p.paragraph_format.space_after=Pt(0)
                for r in p.runs:r.font.name='Times New Roman';r.font.size=Pt(font);set_color(r);r.bold=(rid==0)
    for c in t.rows[0].cells:
        tcPr=c._tc.get_or_add_tcPr(); sh=OxmlElement('w:shd');sh.set(qn('w:fill'),'ECECEC');tcPr.append(sh)
    return t

def code(d,s):
    for line in s.strip('\n').splitlines():
        p=d.add_paragraph(style='CodeLab'); p.add_run(line)

def title(d):
    for line in ['ПРАКТИЧНА РОБОТА №10', 'РОЗРОБКА ВЛАСНОГО ДОДАТКА НА C# ТА ПЛАТФОРМІ .NET', 'ЗГІДНО З ПРЕДМЕТНОЮ ОБЛАСТЮ']:
        p=d.add_paragraph();p.alignment=WD_ALIGN_PARAGRAPH.CENTER
        r=p.add_run(line);r.font.name='Times New Roman';r.font.size=Pt(15);r.bold=True;set_color(r)
    p=d.add_paragraph();p.alignment=WD_ALIGN_PARAGRAPH.CENTER
    r=p.add_run('Тема курсового проєкту: «Облік прокату спортивного інвентарю»');r.bold=True;set_color(r)
    p=d.add_paragraph();p.alignment=WD_ALIGN_PARAGRAPH.CENTER
    p.add_run('Варіант 12. Навчальний консольний прототип на .NET 8.')


def main_report():
    d=base_doc();title(d)
    h(d,'10.1','Мета роботи')
    text(d,'Інтегрувати результати практичних робіт №3–9 у єдину консольну програму для обліку прокату спортивного інвентарю. Реалізувати роботу з клієнтами, інвентарем, оформленням і поверненням оренди, звітами, тарифами та збереженням даних між запусками.')
    h(d,'10.2','Вихідні дані та рішення')
    text(d,'Я використовую варіант 12 із попередніх практичних робіт. ПР4 задає п’ять робіт A1–A5, ПР5 — сценарій A3 з перевіркою доступності, ПР6 — накопичувачі D1–D4, ПР7 — розрахунок за години з умовами вихідного дня та оренди понад 3 доби, а ПР9 — ієрархію RentalItem, Bike, Skis, Tent.')
    text(d,'У ПР6 тарифи були описані як денні, але для ПР7 і наступних робіт використано погодинні ставки. Для узгодженості з кодом ПР7–9 зберігаю навчальні тарифи 40 грн/год для велосипеда, 55 грн/год для лиж і 35 грн/год для намету. У новому додатку їх можна редагувати; уже оформлені прокати не перераховуються.')
    text(d,'Окремий затверджений розділ 2 курсової записки з нумерованими ФВ мені не був наданий. Тому наведені нижче ідентифікатори — робоча матриця, складена за вимогами ПР3–ПР9. Перед остаточною здачею її потрібно звірити з курсовою запискою.')
    h(d,'10.3','Матриця простежуваності вимог')
    rows=[
    ('ФВ-01','Реєструвати клієнта','DFD2 / A2','ClientService.Register','Т01–Т03','Реалізовано'),
    ('ФВ-02','Шукати та переглядати клієнтів','DFD2 / A2','ClientService.Find / All','Т05','Реалізовано'),
    ('ФВ-03','Редагувати, видаляти клієнта','DFD2 / A2','ClientService.Edit / Delete','Т04, Т26','Реалізовано'),
    ('ФВ-04','Додавати інвентар і змінювати стан','DFD1 / A1','InventoryService.Add / SetMaintenance','Т06, Т21–Т22','Реалізовано'),
    ('ФВ-05','Вести повний список інвентарю','DFD1 / A1','InventoryService.Find / All / Delete','Т06–Т07','Реалізовано'),
    ('ФВ-06','Розраховувати вартість за годинами','DFD3 / A3','RentalService.Quote','Т09–Т14','Реалізовано'),
    ('ФВ-07','Ураховувати вихідний та >72 год','A3 / таблиця рішень','RentalItem.CalculateByDecisionTable','Т09–Т12','Реалізовано'),
    ('ФВ-08','Не видавати недоступний інвентар','DFD3 / IDEF3 XOR','RentalService.Issue','Т15–Т17','Реалізовано'),
    ('ФВ-09','Оформляти активний прокат','DFD3 / A3','RentalService.Issue','Т16, Т20','Реалізовано'),
    ('ФВ-10','Реєструвати повернення й ремонт','DFD4 / A4','RentalService.Return','Т18–Т22','Реалізовано'),
    ('ФВ-11','Звіти з фільтром і підсумком','DFD5 / A5','ReportService.Filter / Summary','Т27','Реалізовано'),
    ('ФВ-12','Зберігати D1–D4 між запусками','DFD D1–D4','JsonRepository<T>','Т28–Т30','Реалізовано'),
    ('ФВ-13','Перевіряти введення','Процеси 1–4','ConsoleInput.TryReadInt','Т02, Т37–Т38','Реалізовано'),
    ('ФВ-14','Розраховувати через похідні класи','ПР9 / A3','Bike/Skis/Tent.CalculateRent','Т13–Т14, Т39–Т40','Реалізовано'),
    ('ФВ-15','Онлайн-бронювання / мобільний додаток','Поза межами ПР3','—','—','Відкладено'),
    ('ФВ-16','SMS-нагадування та GPS','Поза межами ПР3','—','—','Відкладено'),
    ('ФВ-17','Інтернет-оплата й багатофілійність','Поза межами ПР3','—','—','Відкладено')]
    table(d,['ID','Вимога','Процес','Клас / метод','Тест','Стан'],rows,[1.4,4.1,2.2,4.25,1.9,2.0],font=9.8)
    cap(d,'Таблиця 10.1 — Матриця «вимога — процес — код — тест»')
    h(d,'10.4','Структура рішення та шарова архітектура')
    code(d,'''SportsRental.sln
├─ SportsRental.Core
│  ├─ Models          Client, InventoryItem, Rental, Tariff,
│  │                  RentalItem → Bike / Skis / Tent
│  ├─ Repositories    IRepository<T>, JsonRepository<T>
│  ├─ Services        ClientService, InventoryService,
│  │                  RentalService, ReportService, TariffService
│  └─ Analytics       RentalAnalytics (ПР8)
├─ SportsRental.Console
│  ├─ Program.cs      Меню й сценарії користувача
│  ├─ ConsoleInput.cs Перевірка введення
│  └─ Diagnostics     SelfTest.cs
├─ data               JSON-файли D1–D4
├─ docs               Діаграми, звіт, інструкція
└─ README.md''')
    text(d,'У Models зберігаються сутності й інваріанти. Services реалізують сценарії DFD та отримують сховища у конструкторі. Repositories читають і записують JSON, а Console не містить правил розрахунку. Напрям залежностей: Console → Core (сервіси, моделі та сховища). Сам Core не посилається на консольний проєкт.')
    h(d,'10.5','Реалізація моделі, класів і станів')
    d.add_picture(str(DOCS/'PR10_Class_Diagram.png'),width=Cm(16.2))
    cap(d,'Рисунок 10.1 — Структура класів, наслідування та зв’язки')
    text(d,'Основні сутності: Client (D2), InventoryItem (D1), Rental (D3) та Tariff (D4). Rental.ClientId і Rental.InventoryId зв’язують прокат із клієнтом та фізичною одиницею обладнання. Класи Bike, Skis і Tent перевизначають поліморфний CalculateRent(int hours), зберігаючи ієрархію з ПР9.')
    text(d,'Переходи станів інвентарю: «Доступний → Виданий → Доступний», «Доступний → На обслуговуванні → Доступний», «Виданий → На обслуговуванні» після повернення несправного предмета. Повторне повернення заборонено станом RentalStatus.Returned. Стан «Виданий» встановлюється лише під час успішного оформлення прокату.')
    h(d,'10.6','Ключові фрагменти програмного коду')
    text(d,'1. Бізнес-правила ПР7. Коефіцієнт визначається двома умовами; для кожного варіанта є окремий тест.')
    code(d,'''decimal multiplier = (StartsOnWeekend, hours > 72) switch
{
    (true, true)   => Tariff.WeekendMultiplier * Tariff.LongRentalMultiplier,
    (true, false)  => Tariff.WeekendMultiplier,
    (false, true)  => Tariff.LongRentalMultiplier,
    (false, false) => 1m
};
return decimal.Round(Tariff.HourlyRate * hours * multiplier, 2);''')
    text(d,'2. Перевірка стану в моделі інвентарю. Вона не дозволяє оформити два активних прокати одного предмета.')
    code(d,'''public void Issue()
{
    if (Status != EquipmentStatus.Available)
        throw new InvalidOperationException("Інвентар недоступний.");
    Status = EquipmentStatus.Rented;
}''')
    text(d,'3. Сховище. Операції запису й завантаження виконуються через IRepository<T>; базовий захист одного файлу — запис у тимчасовий файл із заміною.')
    code(d,'''string json = JsonSerializer.Serialize(GetAll(), _json);
File.WriteAllText(_path + ".tmp", json);
File.Move(_path + ".tmp", _path, overwrite: true);''')
    text(d,'4. Формування звіту з фільтром за статусом.')
    code(d,'''public (int Count, decimal Total) Summary(RentalStatus? status = null)
{
    var selected = _rentals.GetAll()
        .Where(r => !status.HasValue || r.Status == status.Value).ToArray();
    return (selected.Length, selected.Sum(r => r.Cost));
}''')
    h(d,'10.7','Підсумкова таблиця тестування')
    text(d,'Підготовлено 40 автоматизованих тестів: випадки нормального обслуговування, XOR-відмови, всі чотири комбінації тарифів, граничні значення, редагування, JSON-збереження та можливість повторного завантаження. Нижче наведено контрольні очікування. У цьому середовищі .NET не був доступний, тому фактичний запуск на Visual Studio ще потрібний; не можна вважати ці очікування підтвердженими результатами.')
    tests=[
    ('Т01','Реєстрація клієнта','Створено ID=1','—','Не запускали'),
    ('Т02','Номер телефону "12"','Помилка валідації','—','Не запускали'),
    ('Т03','Дублікат ID клієнта','Відмова','—','Не запускали'),
    ('Т04','Редагування ПІБ','«Іван Коваль»','—','Не запускали'),
    ('Т05','Пошук ID=999','Не знайдено','—','Не запускали'),
    ('Т06','Додати велосипед','Доступний','—','Не запускали'),
    ('Т09','24 год, будній, Bike','960,00 грн','—','Не запускали'),
    ('Т10','24 год, вихідний, Bike','1 152,00 грн','—','Не запускали'),
    ('Т11','96 год, будній, Bike','3 456,00 грн','—','Не запускали'),
    ('Т12','96 год, вихідний, Bike','4 147,20 грн','—','Не запускали'),
    ('Т13','24 год, Skis','1 320,00 грн','—','Не запускали'),
    ('Т14','24 год, Tent','840,00 грн','—','Не запускали'),
    ('Т15','Невідомий клієнт','Відмова','—','Не запускали'),
    ('Т16','Видача 24 год','Активний; Виданий','—','Не запускали'),
    ('Т17','Повторна видача зайнятого','Відмова XOR','—','Не запускали'),
    ('Т18','Повернення','Завершено; Доступний','—','Не запускали'),
    ('Т19','Повторне повернення','Відмова','—','Не запускали'),
    ('Т21','Повернення з дефектом','На обслуговуванні','—','Не запускали'),
    ('Т22','Ремонт завершено','Доступний','—','Не запускали'),
    ('Т23–25','0, 336, 337 год','Відмова / так / відмова','—','Не запускали'),
    ('Т27','2 повернення; звіт','2; 2 112,00 грн','—','Не запускали'),
    ('Т28','Запис D1–D4','4 JSON-файли','—','Не запускали'),
    ('Т29','Повторний запуск','Ті ж клієнти, оренди, стан','—','Не запускали'),
    ('Т30','Зміна тарифу й новий запуск','45 грн/год','—','Не запускали'),
    ('Т31–33','Масиви ПР8','198 / 12 / 192','—','Не запускали'),
    ('Т37–38','Некоректні введення','TryParse повертає false','—','Не запускали'),
    ('Т39–40','override / new','Різні типи диспетчеризації','—','Не запускали')]
    table(d,['Тест','Дія / дані','Очікувано','Фактично','Висновок'],tests,[1.5,4.5,4.5,1.8,2.4],font=9.8)
    cap(d,'Таблиця 10.2 — Контрольні дані; колонка фактичних результатів очікує запуску')
    h(d,'10.8','Інструкція користувача та скриншоти')
    text(d,'Інструкція користувача знаходиться в окремому файлі docs/PR10_User_Guide.docx. Для запуску треба відкрити SportsRental.sln, вибрати SportsRental.Console і натиснути Ctrl+F5. Основний сценарій: створити клієнта, додати інвентар, оформити видачу, перевірити звіт, завершити програму, запустити її знову та переконатися у збереженні записів. Потім виконати повернення.')
    text(d,'Місце для чотирьох фактичних знімків екрана з Visual Studio: (1) головне меню; (2) створення клієнта та інвентарю; (3) успішний прокат і відмова при повторній видачі; (4) звіт та повторне завантаження. Зображення не підміняються макетами, бо для здачі потрібні результати запуску.')
    h(d,'10.9','Git-репозиторій і фіксації')
    text(d,'У підготовленому локальному репозиторії створено п’ять комітів за окремими етапами: ініціалізація рішення, моделі, JSON-сховища й сервіси, інтерфейс, тести та документація. Під час доопрацювання звіту додано наступні фіксації. Код зберігається з .gitignore для bin, obj і персональних JSON-даних.')
    text(d,'Посилання на віддалений репозиторій (додати після публікації): _________________________________.')
    h(d,'10.10','Відкладені функції та обмеження')
    text(d,'У поточний прототип не входять онлайн-бронювання, мобільна програма клієнта, SMS, GPS, платіжний термінал та мережа філій — ці функції були за межами ПР3. Для однієї локальної навчальної копії достатньо JSON, але він не забезпечує спільної транзакції між файлами: при збої живлення після запису одного файла стани можуть розійтися. Для реальної експлуатації я використав би базу даних з транзакціями, авторизацію й журнал дій.')
    h(d,'10.11','Висновок')
    text(d,'У роботі я об’єднав моделі й програмні рішення попередніх практичних робіт в один прототип пункту прокату. У коді є п’ять основних процесів DFD, перевірка станів, таблиця тарифних рішень, поліморфні класи обладнання, окремі сервіси та JSON-сховища. Складено матрицю простежуваності, структуру класів, README та набір інтеграційних тестів. До остаточної здачі треба виконати компіляцію і тести у Visual Studio, додати справжні скриншоти та опублікувати Git-репозиторій.')
    h(d,'10.12','Відповіді на контрольні запитання')
    answers=[
    ('1. Як простежити вимогу?', 'У таблиці 10.1 для кожної ФВ указано відповідний процес DFD, клас і метод реалізації та ID перевірки. Наприклад, ФВ-08 → DFD3 / IDEF3 XOR → RentalService.Issue → Т15–Т17.'),
    ('2. Якому DFD-процесу відповідає сервіс?', 'InventoryService — 1 (A1), ClientService — 2 (A2), RentalService.Issue — 3 (A3), RentalService.Return — 4 (A4), ReportService — 5 (A5). TariffService обслуговує накопичувач D4.'),
    ('3. Де таблиця рішень?', 'У RentalItem.CalculateByDecisionTable() є switch з кортежем (StartsOnWeekend, hours > 72). Чотири комбінації мають тести Т09–Т12.'),
    ('4. Де XOR з IDEF3?', 'У RentalService.Issue перевіряються клієнт і доступність обладнання. Якщо стан не Available або вже є активний прокат, видача відхиляється, інакше створюється запис.'),
    ('5. Які переходи станів дозволені?', 'Available → Rented → Available/Maintenance; Available → Maintenance → Available. Недопустимі переходи завершуються InvalidOperationException. Прокат Active → Returned тільки один раз.'),
    ('6. Що зміниться при переході на БД?', 'Можна замінити реалізацію IRepository<T> і конфігурацію ін’єкції залежностей. Сервіси продовжать працювати з інтерфейсами, а БД забезпечить транзакції й багатокористувацький доступ.'),
    ('7. Чому Core не посилається на Console?', 'Бізнес-логіка не повинна залежати від способу введення. Тоді ту саму модель можна використати в консольному або вебінтерфейсі без дублювання правил.'),
    ('8. Що відкладено?', 'Онлайн-бронювання, оплата, SMS, GPS та багатофілійність. У таблиці 10.1 для них установлено стан «Відкладено» за межами ПР3.')]
    for question,answer in answers:
        p=d.add_paragraph();p.add_run(question+' ').bold=True;p.add_run(answer)
    out=DOCS/'PR10_Variant12_Complete_Report.docx';d.save(out);return out


def user_guide():
    d=base_doc()
    d.styles["Normal"].font.size=Pt(12)
    d.styles["Normal"].paragraph_format.line_spacing=1.1
    d.styles["Normal"].paragraph_format.space_after=Pt(2)
    d.sections[0].top_margin=Cm(1.7)
    d.sections[0].bottom_margin=Cm(1.5)
    p=d.add_paragraph('ІНСТРУКЦІЯ КОРИСТУВАЧА');p.alignment=WD_ALIGN_PARAGRAPH.CENTER
    for r in p.runs:r.bold=True;r.font.size=Pt(15);set_color(r)
    text(d,'Програма: «Облік прокату спортивного інвентарю». Практична робота №10, варіант 12.')
    h(d,'1.','Підготовка та запуск')
    text(d,'Розпакуйте ZIP-архів. Відкрийте SportsRental.sln у Visual Studio 2022 з .NET SDK 8.0. Встановіть запуск SportsRental.Console, виконайте Build Solution і натисніть Ctrl+F5. Програма покаже головне меню. У ньому потрібні пункти 1–6. Дані зберігаються в папці, назва якої виводиться під час запуску.')
    h(d,'2.','Основний сценарій')
    for s in ['1. Пункт 2 → 1: створіть клієнта, вкажіть ПІБ, телефон та (за бажанням) email.',
              '2. Пункт 1 → 1: додайте фізичну одиницю інвентарю (велосипед, лижі або намет).',
              '3. Пункт 3: уведіть ID клієнта, ID інвентарю, тривалість 1–336 год і дату початку; перевірте суму та підтвердьте.',
              '4. Пункт 5: виберіть «Активні» й перевірте запис і суму.',
              '5. Вийдіть (0), знову відкрийте застосунок та перевірте, що записи збережено.',
              '6. Пункт 4: поверніть інвентар за номером прокату. Пункт 5 → «Завершені»: перевірте результат.']:
        text(d,s)
    h(d,'3.','Перевірка помилок і тарифів')
    text(d,'Якщо спробувати повторно видати зайнятий предмет, з’явиться повідомлення про недоступність. Якщо ввести літери замість числа, введення повториться. Пункт 6 показує тарифи й дозволяє змінити їх для нових прокатів. Пункт 7 демонструє поліморфізм. Пункти 8–9 — табуляція та обробка масивів з попередніх робіт.')
    h(d,'4.','Підтвердження для викладача')
    text(d,'Нижче залишено місця для справжніх знімків екрана; макети запуску не додаються. Після перевірки у Visual Studio вставте їх замість підписів.')
    for num,descr in [('1','Головне меню'),('2','Реєстрація клієнта та додавання інвентарю'),('3','Оформлення та XOR-відмова'),('4','Звіт і перевірка повторного запуску')]:
        p=d.add_paragraph(f'Рисунок {num} — {descr}. [ВСТАВИТИ ВЛАСНИЙ СКРИНШОТ]')
        p.paragraph_format.space_after=Pt(8)
    out=DOCS/'PR10_User_Guide.docx';d.save(out);return out

if __name__=='__main__':
    print(main_report());print(user_guide())
