using RayRectangle = Raylib_cs.Rectangle;
using RayColor = Raylib_cs.Color;
//using RayVector2 = System.Numerics.Vector2;

using Raylib_cs;
using System.ComponentModel;
using System.Numerics;
using System.IO;
using System.Text.Json;
using ConsoleApp1;
using System.ComponentModel.DataAnnotations;
using System;
using System.Windows.Forms;
using System.ComponentModel.Design;
class Program
{
    static int Restart(ref int collectedCheckpoints, ref Vector2 position, ref Vector2 velocity, ref int bouncesComplete, ref bool isFinished, ref float episodeTime, List<ILevelObject> allObjects)
    {
        foreach (var obj in allObjects)
        {
            if(obj is Checkpoint cp)
            {
                cp.IsCollected = false;
            }
           
        }
        collectedCheckpoints = 0;
        position = new Vector2(400, 100);
        velocity = Vector2.Zero;
        bouncesComplete = 0;
        isFinished = false;
        episodeTime = 0f;
        return 0;
    }

    //нахождение близжайшего объекта пускает луч
    static float Raycast(Vector2 origin, Vector2 direction, float maxDistance, List<ILevelObject> allObjects, float floorY)
    {
        float closestDist = maxDistance;

        foreach(Platform platform in allObjects)
        {
            Vector2[] corners = new Vector2[]
            {
               new Vector2(platform.Rect.X, platform.Rect.Y),
               new Vector2(platform.Rect.X + platform.Rect.Width, platform.Rect.Y),
               new Vector2(platform.Rect.X, platform.Rect.Y + platform.Rect.Height),
               new Vector2(platform.Rect.X + platform.Rect.Width, platform.Rect.Y + platform.Rect.Height),
            };

            foreach (var corner in corners)
            {
                Vector2 ToCorn = corner - origin;
                float DistToCorn = Vector2.Distance(origin, ToCorn);
                if (DistToCorn < 30) continue;

                if(direction.X == 0 && direction.Y != 0)
                {
                    if(Math.Abs(corner.X - origin.X) < 30 && DistToCorn < closestDist)
                    {
                        if(direction.Y > 0 && corner.Y > origin.Y || direction.Y < 0 && corner.Y < origin.Y)
                        {
                            closestDist = DistToCorn;
                        }
                    }

                }
            }

        }

        if(direction.Y > 0)//дистанция до пола
        {
            float distanceToFloor = floorY - origin.Y;
            if(distanceToFloor > 0 && distanceToFloor < closestDist)
            {
                closestDist = distanceToFloor;
            }
        }

        return closestDist;
    }
    //Сканер верха и низа

    static float[] ScanEnvironment(Vector2 pos, List<ILevelObject> allObj, float floorY)
    {
        float maxDist = 500f;
        float[] distances = new float[2];

        //луч вверх
        distances[0] = Raycast(pos, new Vector2(0, -1), maxDist, allObj, floorY);
        //луч вниз
        distances[1] = Raycast(pos, new Vector2(0, 1), maxDist, allObj, floorY);

        return distances;
    }

    //база ИИ
    static string GetStateGameKey(Vector2 pos, Vector2 vel, RayRectangle? finish, List<ILevelObject> allObj, float floorY, List<Checkpoint> checkpoints, int collectedCount, int streak)
    {
        float[] distances = ScanEnvironment(pos, allObj, floorY); //данные у ИИ
        float upDist = distances[0];
        float downDist = distances[1];

        float nearestCPDist = 999;
        string nearestCPDir = "C";
        foreach(var Cp in checkpoints)
        {
            if (!Cp.IsCollected)
            {
                float dist = Vector2.Distance(pos, new Vector2(Cp.Rect.X, Cp.Rect.Y));
                if (dist < nearestCPDist)
                {
                    nearestCPDist = dist;
                    nearestCPDir = pos.X < Cp.Rect.X ? "R" : "L";
                }
            }
        }

        string cp = nearestCPDist < 100 ? "N" : (upDist < 150 ? "M" : "L");
        string finishDir = finish.HasValue ? (pos.X < finish.Value.X ? "R" : "L") : "C";
        string height = pos.Y > 400 ? "Low" : "Hight";
        string speed = Math.Abs(vel.X) > 200 ? "Fast" : "Slow";
        string up = upDist < 50 ? "N" : (upDist < 150 ? "M" : "F");
        string down = downDist < 50 ? "N" : (downDist < 150 ? "M" : "F");
        return $"{finishDir}_{height}_{nearestCPDir}_{cp}_{collectedCount}_{checkpoints.Count}_{streak}_{speed}_{up}_{down}";
    }

    static int ChooseActionAI(string stateKey, Random random, float explorationRate, Dictionary<string, float> QTable)
    {
        if(random.NextDouble() < explorationRate) //с шансом пробует случайное
        {
            return random.Next(-1, 2);
        }

        //или использует известный лучший вариант
        if (QTable.ContainsKey($"{stateKey}_-1") && QTable.ContainsKey($"{stateKey}_0") && QTable.ContainsKey($"{stateKey}_1"))
        {
            float left = QTable[$"{stateKey}_-1"];
            float stay = QTable[$"{stateKey}_0"];
            float right = QTable[$"{stateKey}_1"];

            if (left > right && left > stay) return -1;
            if (left < right && left < stay) return 1;
        }

        return 0;
    }

    static float CalculateRewardAI(Vector2 oldPos, Vector2 newPos, RayRectangle? finish, int bounces, float timeP)
    {
        float reward = 0f;
        if (finish.HasValue)
        {
            float oldDist = Math.Abs(oldPos.X - finish.Value.X);
            float newDIst = Math.Abs(newPos.X - finish.Value.X);
            reward += (oldDist - newDIst) * 0.1f; //разница в расстояниях
        }

        reward -= bounces * 0.5f;
        reward += (newPos.X - oldPos.X) * 0.1f;
        reward -= timeP * 0.2f;
        return reward;
    }

    static void SaveLevel(string fileName, string name, List<ILevelObject> allObj, RayRectangle? finishLine)
    {
        if (!Directory.Exists("Levels")) //нет папки
        {
            Directory.CreateDirectory("Levels");
        }

        LevelData data = new LevelData { Name = name};
        foreach (var obj in allObj)
        {
            if(obj is Platform p) { data.Platforms.Add(p); }
            if (obj is Checkpoint c) { data.Checkpoints.Add(c); }
        }


        data.FinishLineX = finishLine?.X;
        data.FinishLineY = finishLine?.Y;
        data.FinishLineWidth = finishLine?.Width;
        data.FinishLineHeight = finishLine?.Height;
        

        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText($"Levels/{fileName}.json", json);
        Console.WriteLine($"Уровень сохранён в Levels/{fileName}.json");
    }

    static LevelData? LoadLevel(string fileName)
    {
        string path = $"Levels/{fileName}.json";
        if(!File.Exists(path))
        {
            Console.WriteLine("файл не найден");
            return null;
        }

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<LevelData>(json);
    }

    [STAThread]
    static void Main()
    {
        //ИИ и процесс
        float MinTimeAI = 10;
        int AIWinStreak = 0;
        float MinTimePeople = 10;


        bool isAiMode = false;
        Dictionary<string, float> qTable = new Dictionary<string, float>();
        float explorationRate = 0.3f;
        int aiDirection = 0;
        float lastReward = 0;
        Vector2 lastPos = Vector2.Zero;

        int PeopleBestBounces = 0;

        //окно и процесс
        Raylib.InitWindow(800, 600, "My Mini Engine");
        Raylib.SetTargetFPS(60);

        Raylib.InitAudioDevice();

        RayRectangle? finishLine = null;
        bool isFinished = false;
        float episodeTime = 0f;
        string csvPath = "telemetry.csv";
        string LevelName = "Untitled";
        bool isNamingMode = false;
        string nameImputBuffer = "";

        int bouncesComplete = 0;
        float speed = 1000; // Скорость шара
        float FrictionK = 0.95f; //коэфициент трения
        Vector2 position = new Vector2(400, 100); //400 100
        Vector2 velocity = Vector2.Zero;
        Vector2 gravity = new Vector2( 0, 500); //м/с s500
        float floorY = 550; //пол Y
        float bounceFactor = 0.8f; // возрат прыжка, 1 = полный возрат s0.6
        int collectedCheckpoints = 0;

        float kMatch = 0.3f; //коэфициент масштаба дебажинг(скорость масштаба)

        //редактор
        bool isEditMode = false;
        List<ILevelObject> levelObjects = new List<ILevelObject>();
        //НАСТРОЙКИ РЕДАКТОРА
        const float GRID_SIZE = 32;
        const int PLATFORM_WIDTH = 96;
        const int PLATFORM_HIGHT = 32;

        //игровой цикл
        while (!Raylib.WindowShouldClose())
        {
            List<Checkpoint> checkpointList = new List<Checkpoint>();
            foreach (var checkSc in levelObjects)
            {
                if (checkSc is Checkpoint)
                {
                    checkpointList.Add((Checkpoint)checkSc);
                }
            }


            if (Raylib.IsKeyPressed(KeyboardKey.P))
            {
                isAiMode = !isAiMode;
                Console.WriteLine(isAiMode ? "ИИ включен" : "ИИ выключен");
            }


            //математический старт

            float deltaTime = Raylib.GetFrameTime();
            Vector2 mousePos = Raylib.GetMousePosition();

            //управление в реальном времени
            if (Raylib.IsKeyDown(KeyboardKey.Space)) 
            { 
                if(!isAiMode && bouncesComplete > PeopleBestBounces)
                {
                    PeopleBestBounces = bouncesComplete;
                }
                position = new Vector2(400, 100); 
                velocity = Vector2.Zero; 
                bouncesComplete = 0;
                isFinished = false;
                episodeTime = 0f;
            }

            if (Raylib.IsKeyDown(KeyboardKey.Up) && Raylib.IsKeyDown(KeyboardKey.M)) gravity.Y = 500;
            else if (Raylib.IsKeyDown(KeyboardKey.Up)) gravity.Y -= 10;
            if (Raylib.IsKeyDown(KeyboardKey.Down)) gravity.Y += 10;

            if(episodeTime > 10 && isAiMode)
            {
                position = new Vector2(400, 100);
                velocity = Vector2.Zero;
                bouncesComplete = 0;
                isFinished = false;
                episodeTime = 0f;
            }

            if (Raylib.IsKeyDown(KeyboardKey.B) && Raylib.IsKeyDown(KeyboardKey.Space)) bounceFactor = (int)bounceFactor;
            if (Raylib.IsKeyDown(KeyboardKey.B) && Raylib.IsKeyDown(KeyboardKey.M)) bounceFactor = 0.8f;
            float stepBounceFactor = Raylib.IsKeyDown(KeyboardKey.LeftShift) ? 0.01f : 0.001f;
            if (Raylib.IsKeyDown(KeyboardKey.B) && Raylib.IsKeyDown(KeyboardKey.Equal)) bounceFactor = Math.Clamp(bounceFactor + stepBounceFactor, 0, 1.5f);
            if (Raylib.IsKeyDown(KeyboardKey.B) && Raylib.IsKeyDown(KeyboardKey.Minus)) bounceFactor = Math.Clamp(bounceFactor - stepBounceFactor, 0, 1.5f);

            //РЕДАКТОР
            if (Raylib.IsKeyDown(KeyboardKey.Tab)) isEditMode = !isEditMode;
            if(isEditMode)
            {
                //ввод имени файла сохранения

                if (isNamingMode)
                {
                    int key = Raylib.GetCharPressed();

                    while(key > 0)
                    {
                        if(key >= 32 && key <= 125 && nameImputBuffer.Length < 30)
                        {
                            nameImputBuffer += (char)key;
                        }

                        key = Raylib.GetCharPressed();
                    }

                    //закончить ввод
                    if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                    {
                        if(nameImputBuffer.Length > 0)
                        {
                            LevelName = nameImputBuffer;
                        }

                        isNamingMode = false;
                        nameImputBuffer = "";
                    }

                    //отмена 
                    if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                    {
                        isNamingMode = false;
                        nameImputBuffer = "";
                    }

                    if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
                    {
                        if(nameImputBuffer.Length > 0)
                        {
                            nameImputBuffer = nameImputBuffer.Substring(0, nameImputBuffer.Length - 1);//берём всё кроме последнего символа, тоесть фактически его удаляя
                        }
                    }
                }

                //сохранение

                if (Raylib.IsKeyPressed(KeyboardKey.S) && Raylib.IsKeyDown(KeyboardKey.LeftControl))
                {
                    if (!isNamingMode)
                    {
                        isNamingMode = true;
                        nameImputBuffer = LevelName;
                    }
                    else
                    {
                        string safeFileName = LevelName.Replace(" ", "_").Replace("/", "-");//заменяем пробелы и /
                        SaveLevel(safeFileName, LevelName, levelObjects, finishLine);
                        isNamingMode = false;
                    }
                }

                //загрузка

                if (Raylib.IsKeyPressed(KeyboardKey.L) && Raylib.IsKeyDown(KeyboardKey.LeftControl))
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Title = "Выберите уровень для загрузки";
                    openFileDialog.Filter = "JSON файлы|*.json|Все файлы|*.*";
                    string levelsPath = Path.GetFullPath("Levels");

                    if(Directory.Exists(levelsPath))
                    {
                        openFileDialog.InitialDirectory = levelsPath;
                    }

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = openFileDialog.FileName;
                        string fileName = Path.GetFileNameWithoutExtension(filePath);

                        LevelData? loaded = LoadLevel(fileName);
                        if (loaded != null)
                        {
                            levelObjects.Clear();

                            if (loaded.Platforms != null)
                            {
                                foreach (var p in loaded.Platforms)
                                {
                                    levelObjects.Add(p);
                                }
                            }
                            if (loaded.Checkpoints != null)
                            {
                                foreach (var c in loaded.Checkpoints)
                                {
                                    levelObjects.Add(c);
                                }
                            }

                            if (loaded.FinishLineX.HasValue && loaded.FinishLineY.HasValue && loaded.FinishLineWidth.HasValue && loaded.FinishLineHeight.HasValue)
                            {
                                finishLine = new RayRectangle(
                                    loaded.FinishLineX.Value, loaded.FinishLineY.Value,
                                    loaded.FinishLineWidth.Value, loaded.FinishLineHeight.Value
                                );
                            }
                            else
                            {
                                finishLine = null;
                            }

                            LevelName = loaded.Name;
                            Console.WriteLine($"уровень '{loaded.Name}' загружен");
                        }
                    }
                }

                //спавн
                Vector2 snappedPos = new Vector2(MathF.Round(mousePos.X / GRID_SIZE) * GRID_SIZE, MathF.Round(mousePos.Y / GRID_SIZE) * GRID_SIZE);

                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    if (Raylib.IsKeyDown(KeyboardKey.F))
                    {
                        finishLine = new RayRectangle(snappedPos.X, snappedPos.Y, 60, 100);
                    }else if (Raylib.IsKeyDown(KeyboardKey.C))
                    {
                        levelObjects.Add(new Checkpoint(snappedPos.X, snappedPos.Y, GRID_SIZE * 2, GRID_SIZE * 2));
                    }
                    else
                    {
                        levelObjects.Add(new Platform(snappedPos.X, snappedPos.Y, GRID_SIZE * 2, GRID_SIZE / 2));
                    }

                }


                if (Raylib.IsMouseButtonPressed(MouseButton.Right))
                {
                    if(finishLine.HasValue && Raylib.CheckCollisionPointRec(mousePos, finishLine.Value))
                    {
                        finishLine = null;
                    }
                    else
                    {
                        for (int i = levelObjects.Count - 1; i >= 0; i--)
                        {
                            if (Raylib.CheckCollisionPointRec(mousePos, levelObjects[i].Bounds))
                            {
                                levelObjects.RemoveAt(i);
                                break;
                            }
                        }
                    }  
                }
            }

   //ФИЗИКА РЕДАКТОРА
            if (!isEditMode)
            {
                string WinOrNotText = "";
                episodeTime += deltaTime;
                //столкновение с финишем
                if (!isFinished)
                {
                    if(finishLine.HasValue && Raylib.CheckCollisionCircleRec(position, 20, finishLine.Value))
                    {
                        if(checkpointList.Count > 0 && collectedCheckpoints == 0)
                        {
                            Console.WriteLine("надо собрать хоть одну контрольную точку");
                            WinOrNotText = "Проигрышь";
                            Restart(ref collectedCheckpoints, ref position, ref velocity, ref bouncesComplete, ref isFinished, ref episodeTime, levelObjects);
                        }

                        isFinished = true;

                        if (isAiMode && collectedCheckpoints == checkpointList.Count)
                        {
                            AIWinStreak++;
                            string stateKey = GetStateGameKey(position, velocity, finishLine, levelObjects, floorY, checkpointList, collectedCheckpoints, AIWinStreak);
                            qTable[$"{stateKey}_{aiDirection}"] += 1000;
                            explorationRate -= 0.1f * (episodeTime / 1.5f);
                            WinOrNotText = "Выигрышь";
                        }
                        else
                        {
                            AIWinStreak = 0;
                        }

                            bool FilesIsEmpty = !File.Exists(csvPath) || File.ReadAllText(csvPath).Trim() == "";

                        if (FilesIsEmpty)
                        {
                            File.WriteAllText(csvPath, "Робот ли; Отскоки человек; Уровень; Время (сек); Количество отскоков; Случайность; Статус;\n");
                        }

                        if(episodeTime < MinTimeAI && isAiMode)
                        {
                            MinTimeAI = episodeTime;
                        }

                        if (episodeTime < MinTimePeople && !isAiMode)
                        {
                            MinTimePeople = episodeTime;
                        }

                        string newLine = $"{isAiMode}; {PeopleBestBounces}; {LevelName:F2}; {episodeTime:F2}; {bouncesComplete}; {explorationRate}; {WinOrNotText} \n";
                        File.AppendAllText(csvPath, newLine);

                        Console.WriteLine($"\nданные сохранены в telemetry.csv. Лучшая скорость на данный момент при выигрыше AI = {MinTimeAI}. Человек = {MinTimePeople}\n");
                        Restart(ref collectedCheckpoints, ref position, ref velocity, ref bouncesComplete, ref isFinished, ref episodeTime, levelObjects);
                    }
                }
                else
                {
                    AIWinStreak--;
                    explorationRate += 0.1f;
                }

                //столкновения с контрольными точками
                for(int i = levelObjects.Count - 1; i >= 0; i--)
                {
                    var obj = levelObjects[i];

                    if(obj.CheckCollision(position, 20))
                    {
                        bool shouldRemove = obj.OnCollisionWithBallAndAditionActions(position, 20);
                        if(shouldRemove)
                        {
                            levelObjects.RemoveAt(i);
                        }

                        if(obj is Checkpoint cp && cp.IsCollected)
                        {
                            collectedCheckpoints++;
                        }
                    }
                }



                    foreach (var obj in levelObjects)
                    {

                    if(obj is Platform plat)
                    {
                        bool collisionX = position.X + 20 > plat.Rect.X && position.X - 20 < plat.Rect.X + plat.Rect.Width;
                        bool collisionY = position.Y + 20 > plat.Rect.Y && position.Y - 20 < plat.Rect.Y + plat.Rect.Height;

                        if (collisionX && collisionY)
                        {
                            Vector2 prevPos = position - velocity * deltaTime;
                            //сверху
                            if (prevPos.Y + 20 <= plat.Rect.Y + 5 && velocity.Y > 0)
                            {
                                position.Y = plat.Rect.Y - 20;
                                velocity.Y *= -bounceFactor;
                                velocity.X *= FrictionK;
                                // bouncesComplete += 1;
                            }
                            else if (prevPos.Y - 20 >= plat.Rect.Y - 5 && velocity.Y < 0) //снизу
                            {
                                position.Y = plat.Rect.Y + plat.Rect.Height + 20;
                                velocity.Y *= -bounceFactor * 0.5f;
                                bouncesComplete += 1;
                            }
                            else
                            {
                                if (prevPos.X + 20 <= plat.Rect.X + 5)
                                {
                                    position.X = plat.Rect.X - 20;
                                }
                                else if (prevPos.X - 20 <= plat.Rect.X + plat.Rect.Height - 5)
                                {
                                    position.X = plat.Rect.X + plat.Rect.Width + 20;
                                }
                                velocity.X *= -bounceFactor * 0.3f;

                            }
                        }
                    }
                        
                    }



                velocity += gravity * deltaTime;
                position += velocity * deltaTime;

                if (position.Y > floorY)
                {
                    position.Y = floorY;
                    velocity.Y *= -bounceFactor;

                    if (Math.Abs(velocity.Y) < 25)
                    {
                        velocity.Y = 0;
                    }
                }

                //ИИ и человеческое движение

                float moveInput = 0f;

               

                if (isAiMode)
                {
                    string stateKey = GetStateGameKey(position, velocity, finishLine, levelObjects, floorY, checkpointList, collectedCheckpoints, AIWinStreak);

                    Random rand = new Random();
                    aiDirection = ChooseActionAI(stateKey, rand, explorationRate, qTable);
                    moveInput = aiDirection;

                    lastReward = CalculateRewardAI(lastPos, position, finishLine, bouncesComplete, episodeTime);

                    //Q-Learning
                    string prevStateKey = GetStateGameKey(position, velocity, finishLine, levelObjects, floorY, checkpointList, collectedCheckpoints, AIWinStreak);
                    string actionKey = $"{prevStateKey}_{aiDirection}";

                    if(qTable.ContainsKey(actionKey))
                    {
                        qTable[actionKey] += 0.1f * (lastReward - qTable[actionKey]);
                    }
                    else
                    {
                        qTable[actionKey] = lastReward;
                    }

                    lastPos = position;

                    if(!isFinished) Console.WriteLine($"Текущая награда {lastReward} за {qTable.Count} ситуаций. Время -  {episodeTime}");
                }

                if (Raylib.IsKeyDown(KeyboardKey.D)) moveInput = 1;
                if (Raylib.IsKeyDown(KeyboardKey.A)) moveInput = -1;

                Vector2 moveForce = new Vector2(moveInput * speed, 0);
                velocity += moveForce * deltaTime;
                if(position.Y > floorY - 1) velocity.X *= FrictionK;
                //границы

                if (position.X < 20) { position.X = 20; velocity.X *= 0.5f; }
                if (position.X > 780) { position.X = 780; velocity.X *= 0.5f; }

            }

  //отрисовка
            Raylib.BeginDrawing();
            Raylib.ClearBackground(RayColor.Black);
            Raylib.DrawCircleV(position, 20, RayColor.White); //шар
            Raylib.DrawLine(0, (int)floorY, 800, (int)floorY, RayColor.Gray); //пол

            //отрисовка человеческой траектории

            if (!isAiMode)
            {
                Vector2 bouncePoint = position;
                bool willHitFloor = false;
                if (velocity.Y > 0 && position.Y < floorY - 1)
                {
                    float timeToImpact = (floorY - position.Y) / velocity.Y;
                    float impactX = position.X + velocity.X * timeToImpact;
                    bouncePoint = new Vector2(impactX, floorY);
                    willHitFloor = true;
                }
                Raylib.DrawLineEx(position, bouncePoint, 3f, RayColor.Red);

                if (willHitFloor)
                {
                    Vector2 postBounceVelocity = new Vector2(velocity.X, -velocity.Y * bounceFactor);
                    Vector2 postBounceEnd = bouncePoint + postBounceVelocity * kMatch;
                    Raylib.DrawLineEx(bouncePoint, postBounceEnd, 3, RayColor.Lime);
                    Raylib.DrawCircleV(bouncePoint, 4, RayColor.White);
                }
            }

            

            //рисование платформ

            foreach(var obj in levelObjects)
            {
                obj.Draw();
            }

            //рисование контрольных точек

            foreach (var obj in levelObjects)
            {
                obj.Draw();
            }

            //рисование лучей ии

            if (isAiMode)
            {
                float[] distances = ScanEnvironment(position, levelObjects, floorY);

                //луч вверх
                Vector2 upEnd = position + new Vector2(0, -distances[0]);
                Raylib.DrawLineEx(position, upEnd, kMatch, RayColor.Magenta);
                Raylib.DrawCircleV(upEnd, 5, RayColor.Magenta);

                //луч вниз
                Vector2 downEnd = position + new Vector2(0, distances[1]);
                Raylib.DrawLineEx(position, downEnd, kMatch, RayColor.Orange);
                Raylib.DrawCircleV(downEnd, 5, RayColor.Orange);
            }

            //рисование финиша

            if (finishLine.HasValue)
            {
                RayColor finishColor = isFinished ? RayColor.Gold : RayColor.Green;
                Raylib.DrawRectangleRec(finishLine.Value, finishColor);
                Raylib.DrawRectangleLinesEx(finishLine.Value, 2, RayColor.White);
            }


            //рисование сетки

            if (isEditMode)
            {
                for (int x = 0; x < 800; x += (int)GRID_SIZE) Raylib.DrawLine(x, 0, x, 600, new RayColor(30, 30, 30, 255));
                for (int y = 0; y < 800; y += (int)GRID_SIZE) Raylib.DrawLine(0, y, 800, y, new RayColor(30, 30, 30, 255));
            }

            //прицел

            if(isEditMode)
            {
                Vector2 snap = new Vector2(MathF.Round(mousePos.X / GRID_SIZE) * GRID_SIZE, MathF.Round(mousePos.Y / GRID_SIZE) * GRID_SIZE);
                Raylib.DrawRectangleLines((int)snap.X, (int)snap.Y, PLATFORM_WIDTH, PLATFORM_HIGHT, RayColor.Yellow);
            }

            //UI

            int fps = Raylib.GetFPS();
            RayColor fpsColor = fps >= 55 ? RayColor.Lime : (fps >= 30 ? RayColor.Yellow : RayColor.Red);

            Raylib.DrawText($"FPS: {fps}", 10, 40,20, fpsColor);
            Raylib.DrawText($"velocity - {velocity.Y :F2}, gravityY - {gravity.Y :F1}, bounceFactor - {bounceFactor :F2}", 10, 10, 20, RayColor.Lime);
            Raylib.DrawText($"Bounces DO: {bouncesComplete}", 10, 70, 20, RayColor.Green);
            if (position.Y >= floorY - 1f && Math.Abs(velocity.Y) > 5f)
            {
                Raylib.DrawText("Bounce!", (int)position.X, (int)position.Y, 20, RayColor.Orange);
                bouncesComplete += 1;
            }
            Raylib.EndDrawing();

            //отрисовка режима ввода
            if (isNamingMode)
            {
                Raylib.DrawRectangle(200, 250, 400, 50, RayColor.Black);
                Raylib.DrawRectangleLines(200, 250, 400, 50, RayColor.Yellow);
                Raylib.DrawText("Введите название уровня: ", 210, 260, 20, RayColor.White);
                Raylib.DrawText(nameImputBuffer + "_", 210, 285, 20, RayColor.Yellow);
            }

        }
        Raylib.CloseWindow();
    }
}
