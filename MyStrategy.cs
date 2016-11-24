using Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk.Model;
using System;
using System.Collections;

namespace Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk
{
    public sealed class MyStrategy : IStrategy
    {
        double curentAngle, prevLife = 100; //óãîë äâèæåíèÿ äî êîëëèçèè
        bool collisionFlag = false, retreat = false, inAtackZone=true; //ôëàãè ñîñòîÿíèé   
        double[] checkPoint = new double[2] { 500, 300 };
        double[] lastPoint = new double[2];
        double[] retreatCoor = new double[2] { 1400, 1400 };
        int stage = 0;

        void collisionTurn(ref Move move, bool flag, double angle)
        {
            collisionFlag = flag;
            move.Turn = angle;
        }
        double getAngleTo(Wizard delf, CircularUnit unit, bool type)
        {
            double res = delf.GetAngleTo(unit);
            if (type)
                return res;
            else
                if (res <= 0)
                return res + Math.PI;
            else
                return res - Math.PI;

        }
        void movementTo(World world, Wizard self, ref Move move, double[] position, double movementSpeed, bool movementType)
        {
            collisionFlag = false;
            curentAngle = self.Angle + self.GetAngleTo(position[0], position[1]);
            double discrepancy = -self.GetAngleTo(position[0], position[1]);
            ArrayList objectsInView = new ArrayList();
            double angle;
            if (movementType)
                move.Speed = movementSpeed;
            else
            {
                if (self.Angle <= 0)
                {
                    discrepancy = -discrepancy;
                    curentAngle = curentAngle + Math.PI;
                }
                else
                {
                    discrepancy = -discrepancy;
                    curentAngle = curentAngle - Math.PI;
                }
                move.Speed = -movementSpeed;
            }

            //îáíàðóæåíèå êîëëèçèé
            foreach (Building building in world.Buildings)
                objectsInView.Add(building);
            foreach (Tree obj in world.Trees)
                objectsInView.Add(obj);
            foreach (Minion minions in world.Minions)
                if (minions.Faction == self.Faction || minions.Faction==Faction.Neutral)  // îãèáàòü òîëüêî ñâîèõ êðèïîâ
                    objectsInView.Add(minions);
            foreach (Wizard wizard in world.Wizards)
                if (!wizard.IsMe)
                    objectsInView.Add(wizard);
            //êîððåêòèðîâêà êîëëèçèé
            foreach (CircularUnit obj in objectsInView)
                if ((obj.GetDistanceTo(self) - self.Radius - obj.Radius) < 50)
                {
                    angle = getAngleTo(self, obj, movementType);
                    if (angle >= 0 && angle <= 1.5)
                        collisionTurn(ref move, true, -3);
                    if (angle < 0 && angle >= -1.5)
                        collisionTurn(ref move, true, 3);
                    if (angle > 1.5)
                        collisionTurn(ref move, true, 0);
                    if (angle < -1.5)
                        collisionTurn(ref move, true, 0);
                }
            if (self.X - self.Radius < 50 || self.X - self.Radius > 3950 || self.Y - self.Radius < 50 || self.Y - self.Radius > 3950)
                collisionTurn(ref move, true, 3);
            if (!collisionFlag && discrepancy > 0.2)
                collisionTurn(ref move, false, -3);
            if (!collisionFlag && discrepancy < 0.2)
                collisionTurn(ref move, false, 3);
            if (!collisionFlag && discrepancy >= -0.2 && discrepancy <= 0.2)
                collisionTurn(ref move, true, 0);
        }
        ArrayList findEnemy(World world, Wizard self)
        {
            double distToMinion = 2000, distToWizard = 2000, distToTower=2000, minMinionHP = 100, minWizardHP = 100, dist, count=0;
            Minion nearestMinion = null, minionWithoutHP = null;
            Wizard nearestWizars = null, wizardWithoutHP = null;
            Building nearestBuilding = null;

            inAtackZone = false;
            foreach (Minion minion in world.Minions)
            {
                if (minion.Faction == self.Faction && self.GetAngleTo(minion) < 1.4 && self.GetAngleTo(minion) > -1.4)
                    count++;
                if (minion.Faction != self.Faction && minion.Faction != Faction.Neutral)
                {
                    dist = self.GetDistanceTo(minion);
                    if (dist <= distToMinion && dist <= self.CastRange)
                    {
                        distToMinion = dist;
                        nearestMinion = minion;
                    }
                    if (minion.Life <= minMinionHP && dist <= self.CastRange)
                    {
                        minMinionHP = minion.Life;
                        minionWithoutHP = minion;
                    }
                    if (dist-100 <= minion.VisionRange)
                        inAtackZone = true;
                }
            }
            foreach (Wizard wizard in world.Wizards)
            {
                if (wizard.Faction != self.Faction)
                {
                    dist = self.GetDistanceTo(wizard);
                    if (dist <= distToWizard && dist <= 490)
                    {
                        distToWizard = dist;
                        nearestWizars = wizard;
                    }
                    if (wizard.Life <= minWizardHP && dist <= self.CastRange)
                    {
                        minWizardHP = wizard.Life;
                        wizardWithoutHP = wizard;
                    }
                    if (dist - 100 <= wizard.CastRange)
                        inAtackZone = true;
                }
            }
            foreach (Building building in world.Buildings)
            {
                if (building.Faction != self.Faction && self.GetDistanceTo(building) <= self.CastRange)
                {
                    count++;
                    nearestBuilding = building;
                    distToTower = self.GetDistanceTo(building);
                    if (distToTower - 100 <= building.VisionRange)
                        inAtackZone = true;
                }
            }
            return new ArrayList() { nearestMinion, minionWithoutHP, distToMinion, minMinionHP, nearestWizars, wizardWithoutHP, distToWizard, minWizardHP, nearestBuilding, distToTower, count};
        }
        void atackEnemyWizard(Wizard wizard, Wizard self, World world, ref Move move)
        {
            double angleToEnemy = self.GetAngleTo(wizard);
            move.Speed = 0; //stop moving 
            if (angleToEnemy >= 0)//aiming
                move.Turn = 0.05;
            else
                move.Turn = -0.05;

            if (angleToEnemy > -0.1 && angleToEnemy < 0.1 && self.RemainingActionCooldownTicks == 0)//start shooting
                move.Action = ActionType.MagicMissile;
        }
        void atackEnemyMinion(Minion minion, Wizard self, World world, ref Move move)
        {
            double angleToEnemy = self.GetAngleTo(minion);
            move.Speed = 0; //stop moving 
            if (angleToEnemy >= 0)//aiming
                move.Turn = 0.05;
            else
                move.Turn = -0.05;
            if (angleToEnemy > -0.15 && angleToEnemy < 0.15 && self.RemainingActionCooldownTicks == 0)//start shooting
                move.Action = ActionType.MagicMissile;
        }
        void atackEnemyBuilding(Building building, Wizard self, World world, ref Move move)
        {
            double angleToEnemy = self.GetAngleTo(building);
            move.Speed = 0; //stop moving 
            if (angleToEnemy >= 0)//aiming
                move.Turn = 0.05;
            else
                move.Turn = -0.05;
            if (angleToEnemy > -0.1 && angleToEnemy < 0.1 && self.RemainingActionCooldownTicks == 0)//start shooting
                move.Action = ActionType.MagicMissile;
        }
        void movementControl(ref Move move, Wizard self, World world, ref double speed, double distanceToCheckPoint)
        {
            ArrayList scanResult;
            scanResult = findEnemy(world, self); //àòàêîâàòü èëè îòñòóïàòü
            double angle = self.GetAngleTo(retreatCoor[0], retreatCoor[1]);
            retreat=false;

            if (scanResult[0] != null) // àòàêîâàòü áëèæíåãî ìèíüîíà
            {
                atackEnemyMinion((Minion)scanResult[0], self, world, ref move);
                if ((double)scanResult[3] <= 24) //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    atackEnemyMinion((Minion)scanResult[1], self, world, ref move);
                retreat = false;
            }

            if (scanResult[4] != null) //  atack wizard
            {
                atackEnemyWizard((Wizard)scanResult[4], self, world, ref move);
                if ((double)scanResult[7] <= 24) 
                    atackEnemyWizard((Wizard)scanResult[5], self, world, ref move);
                retreat = false;
            }

            if (scanResult[8] != null)
                atackEnemyBuilding((Building)scanResult[8], self, world, ref move);

            if ((scanResult[4] != null && (double)scanResult[6] < 300) || (scanResult[0] != null && (double)scanResult[2] < 300) || (scanResult[8] != null && (double)scanResult[9] < 450))  //побег от ближнего контакта
            {
                Minion min = (Minion)scanResult[0];
                speed = 4;
                movementTo(world, self, ref move, retreatCoor, speed, false);
                retreat = true;
                if (angle < 1.57 && angle > -1.57)
                    movementTo(world, self, ref move, retreatCoor, speed, true);
                move.Action = ActionType.MagicMissile;
            }

            if (self.Life < 50)
            {
                if (angle < 1.57 && angle > -1.57)
                {
                    movementTo(world, self, ref move, retreatCoor, speed, true);
                }
                else
                    movementTo(world, self, ref move, retreatCoor, 4, false);
                retreat = true;
                if (!inAtackZone)
                {
                    retreat = false;
                    move.Speed = 0;
                }
            }

            //if (self.Life > 55)
              //  retreat = false;

            if (scanResult[0] == null && scanResult[4] == null && !retreat) // moving
            {
                movementTo(world, self, ref move, checkPoint, speed, true);
                if (distanceToCheckPoint < 50 && !retreat)
                    stage++;
            }

            if (self.Life - prevLife > 60)
            {
                stage = 0;
                retreat = false;
            }
            //Console.WriteLine($"{scanResult[10]} {retreat} {scanResult[2]}");
        }

        public void Move(Wizard self, World world, Game game, Move move)
        {
            double distanceToCheckPoint;
            double distanceToRetreatPoint;
            double speed = 0, angleToRetreat = self.GetAngleTo(retreatCoor[0], retreatCoor[1]);

            if (world.TickIndex > 100)
            {
                retreatCoor[0] = 600;
                retreatCoor[1] = 3900;
                speed = 3;
                switch (stage) // хождение по чекпоинтам
                {
                    case 0:
                        checkPoint[0] = 200;
                        checkPoint[1] = 3800;
                        break;
                    case 1:
                        checkPoint[0] = 500;
                        checkPoint[1] = 3800;
                        break;
                    case 2:
                        checkPoint[0] = 500;
                        checkPoint[1] = 3500;
                        break;                    
                    default:
                        break;
                }
                if (stage > 2)
                {
                    checkPoint[0] = checkPoint[0] + 150;
                    checkPoint[1] = checkPoint[1] - 150;
                }

                distanceToCheckPoint = self.GetDistanceTo(checkPoint[0], checkPoint[1]) - self.Radius;
                distanceToRetreatPoint = self.GetDistanceTo(retreatCoor[0], retreatCoor[1]) - self.Radius;
                movementControl(ref move, self, world, ref speed, distanceToCheckPoint);
            }
        }
    }
}