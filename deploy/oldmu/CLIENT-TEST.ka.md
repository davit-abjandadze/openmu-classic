# Old MU — რეალური კლიენტით ტესტი გაშვებამდე

თითოეულ პუნქტს მონიშნე `[x]`-ით. თუ რამე არ მუშაობს, ჩაწერე რა გააკეთე, რა
მოხდა და რას ელოდი. სერვერის ლოგი: `src/Startup/bin/Debug/.../logs`, კლიენტის
ლოგი: `%APPDATA%\MuMain\Client-*\MuError.log`.

ტესტი გაიარე **ორი ანგარიშით, ორი კლიენტით** (party, trade, duel, PvP), ერთი
მათგანი GM (`testgm`), მეორე ჩვეულებრივი (`test300`, `test0` … `test9`).

## 1. შესვლა და პერსონაჟი
- [ ] launcher → login → server select → სერვერის სია ორივე game server-ით
- [ ] ახალი DK, DW, Elf და MG იქმნება; DL / Summoner / RF ღილაკები არ ჩანს
- [ ] ახალ პერსონაჟს სწორი საწყისი ნივთები და ინვენტარი აქვს
- [ ] გასვლა და ხელახლა შესვლა: ინვენტარი, ზენი, level, სტატები შენახულია
- [ ] სერვერის გადართვა (Server 0 → Server 1) ნივთების დაკარგვის გარეშე

## 2. Level, სტატები, reset
- [ ] EXP 100x შეესაბამება მოლოდინს (რამდენ ხანში ადის 1→100, 100→200)
- [ ] `/addstr`, `/addagi`, `/addvit`, `/addene`, `/add str 100` მუშაობს
- [ ] `/reset` 350+ level-ზე; reset-ის შემდეგ level, წერტილები და EXP rate სწორია
- [ ] `/resetinfo` სწორ ინფორმაციას აჩვენებს
- [ ] 2nd class quest (BK / SM / ME / MG) ბოლომდე გადის
- [ ] master skill tree-ს ღილაკი არ ჩანს, `A` არაფერს ხსნის

## 3. რუკები და warp
- [ ] `M` warp სიაში არ არის Elvenland, Aida, Kanturu, Karutan, Raklion, Peace Swamp, Vulcanus
- [ ] `/move` ამ რუკებზე უარს ამბობს
- [ ] Lorencia, Devias, Noria, Dungeon, Atlans, Lost Tower, Tarkan, Icarus: monster-ები და farm spot-ები ადგილზეა
- [ ] `Tab` რუკა მთლიანად ჩანს, ბოსები და respawn-ის ტაიმერი ჩანს

## 4. Drop და ნივთები
- [ ] jewel-ები (Bless, Soul, Chaos, Life) ვარდება ~2%-ით
- [ ] excellent ნივთები ვარდება; Tarkan / Icarus-ში მხოლოდ მაღალი დონის excellent
- [ ] ring / pendant იშვიათია (ჩვეულებრივი 0.5%, excellent 0.02%)
- [ ] Box of Heaven ვარდება და +7…+11 ნივთს აძლევს
- [ ] ზენის რაოდენობა მიწაზე და აღებისას ერთნაირია (500x)
- [ ] ნივთის +12-ზე აწევა უარყოფილია (max +11)
- [ ] Chaos Machine: Chaos Weapon, Wings, +10/+11 mix

## 5. ბოსები
- [ ] `/bosses` სიას და respawn-ის დროს აჩვენებს
- [ ] Hydra, Balrog, Zaikan, Death Beam Knight, Dark Phoenix: გარანტირებული drop (Kundun box + silver medal)
- [ ] respawn-ის დრო ემთხვევა `/bosses`-ს და `Tab` რუკას

## 6. ივენთები
GM-ს შეუძლია `/startds` და `/startbc`; შეამოწმე, ეს ახალი instance-ებიან
ივენთს იწყებს თუ ძველს. თუ ძველს, დაელოდე განრიგს (DS 13:00 / 19:00 / 22:00,
BC +30 წთ, თბილისის დრო).
- [ ] 5 და 2 წუთით ადრე გამოცხადება
- [ ] `/joinds`, `/joinbc` სოლოდ და party-ით (leader შეჰყავს ყველას)
- [ ] სწორი tier level-ის მიხედვით; party-ში უმცირესი level ითვლება
- [ ] დღეში ერთხელ თითო ივენთზე; მეორე მცდელობაზე უარი
- [ ] ერთი IP-დან მხოლოდ ერთი ანგარიში ერთ ივენთში
- [ ] მონსტრების რაოდენობა და HP მოთამაშეების რაოდენობით იზრდება
- [ ] ჯილდოები: EXP, silver medal ჩანთაში (სავსე ჩანთისას ატრიბუტში)
- [ ] Golden Invasion 23:00, Golden Tantalos ტოვებს Kundun +4-ს
- [ ] TvT: `/jointvt`, 23:30, Arena; MVP და გამარჯვებული გუნდი medal-ს იღებს

## 7. PvP
- [ ] `/zones` PvP spot-ებს აჩვენებს; კვლა spot-ზე prestige-ს აძლევს
- [ ] duel-ის მოგება prestige-ს აძლევს
- [ ] `/ranking` და `/ranking all`
- [ ] PK სტატუსი, `/pkclear`

## 8. NPC-ები და ვაჭრობა
- [ ] Thompson: silver medal-ით Kundun +3 / +4 (5 / 10 medal)
- [ ] Caren: Kundun +1…+4 ზენით (მხოლოდ ტესტისთვის, გაშვებამდე ამოსაღები)
- [ ] `/exchange` მუშაობს
- [ ] trade ორ მოთამაშეს შორის, personal store, warehouse

## 9. დაცვა და დუპი
- [ ] ერთი IP-დან მესამე ანგარიში ვერ შედის (ორივე სერვერზე ჯამში)
- [ ] `/offlevel` → იგივე ანგარიშით მეორე სერვერზე შესვლა უარყოფილია
- [ ] `/offlevel` → იმავე სერვერზე შესვლა offline სესიას იღებს, ნივთები არ ორმაგდება
- [ ] კლიენტის უცებ დახურვა (Task Manager) trade-ის / mix-ის დროს: ნივთი არ ორმაგდება და არ იკარგება
- [ ] სერვერის გადატვირთვა: ყველა მოთამაშის პროგრესი შენახულია

## 10. Admin panel
- [ ] `/control-center`: rate-ების ცვლილება მოქმედებს გადატვირთვის გარეშე
- [ ] ახალი ანგარიშის შექმნა admin panel-იდან და მისით შესვლა
- [ ] ანგარიშის ban / unban
