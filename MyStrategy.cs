using Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk.Model;
using System;
using System.Collections;

namespace Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk
{
    public sealed class MyStrategy : IStrategy
    {
        double curentAngle, prevLife=100; //угол движения до коллизии
        bool collisionFlag = false, movementFlag = false, retreat = false; //флаги состояний   
        double match = 0;
        double[] checkPoint = new double[2] {500,300};
        double[] lastPoint = new double[2];
        double[] retreatCoor = new double[2] { 1400,1400};
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

            //обнаружение коллизий
            foreach (Building building in world.Buildings)
                objectsInView.Add(building);
            foreach (Tree obj in world.Trees)
                objectsInView.Add(obj);
            foreach (Minion minions in world.Minions)
                //if (minions.Faction == self.Faction)  // огибать только своих крипов
                    objectsInView.Add(minions);
            foreach (Wizard wizard in world.Wizards)
                if (!wizard.IsMe)
                    objectsInView.Add(wizard);
            //корректировка коллизий
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

            //позволяет избежать избиение стены лбом
            if (self.X == lastPoint[0] && self.Y == lastPoint[1] && !movementFlag)
            {
                movementFlag = true;
                match = match + 50;
            }
            if (movementFlag)
            {
                move.Speed = -move.Speed;
                match--;
                if (match == 0)
                {
                    move.Speed = -move.Speed;
                    move.Turn = -2;
                    movementFlag = false;
                }
            }
            lastPoint[0] = self.X;
            lastPoint[1] = self.Y;
        }
        ArrayList findEnemy(World world, Wizard self)
        {
            double distToMinion = 2000, distToWizard = 2000, minMinionHP = 100, minWizardHP = 100, dist;
            Minion nearestMinion = null, minionWithoutHP = null;
            Wizard nearestWizars = null, wizardWithoutHP = null;
            Building nearestBuilding = null;
            foreach (Minion minion in world.Minions)
            {
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
                }
            }
            foreach (Building building in world.Buildings)
            {
                if (building.Faction != self.Faction && self.GetDistanceTo(building) <= self.CastRange)
                    nearestBuilding = building;
                    
            }
            return new ArrayList() { nearestMinion, minionWithoutHP, distToMinion, minMinionHP, nearestWizars, wizardWithoutHP, distToWizard, minWizardHP, nearestBuilding};
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
                move.Turn = 0.025;
            else
                move.Turn = -0.025;
            if (angleToEnemy > -0.1 && angleToEnemy < 0.1 && self.RemainingActionCooldownTicks == 0 && (minion.Life > 24 || minion.Life <= 12))//start shooting
                move.Action = ActionType.MagicMissile;
        }
        void atackEnemyBuilding(Building building, Wizard self, World world, ref Move move)
        {
            double angleToEnemy = self.GetAngleTo(building);
            move.Speed = 0; //stop moving 
            if (angleToEnemy >= 0)//aiming
                move.Turn = 0.025;
            else
                move.Turn = -0.025;
            if (angleToEnemy > -0.1 && angleToEnemy < 0.1 && self.RemainingActionCooldownTicks == 0)//start shooting
                move.Action = ActionType.MagicMissile;
        }
        void movementControl(ref Move move, Wizard self, World world, ref double speed, double distanceToCheckPoint)
        {
            ArrayList scanResult;
            scanResult = findEnemy(world, self); //атаковать или отступать
            double angle = self.GetAngleTo(retreatCoor[0], retreatCoor[1]);

            if (self.Life > 60)
                retreat = false;

            if (scanResult[0] != null && (double)scanResult[2] >= 212) // атаковать ближнего миньона
            {
                atackEnemyMinion((Minion)scanResult[0], self, world, ref move);
                if ((double)scanResult[3] <= 24) //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    atackEnemyMinion((Minion)scanResult[1], self, world, ref move);
                retreat = false;
            }

            if (scanResult[4] != null && (double)scanResult[6] >= 212) // атаковтать ближнего мага
            {
                atackEnemyWizard((Wizard)scanResult[4], self, world, ref move);
                if ((double)scanResult[7] <=24) // ластхит крипа !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    atackEnemyWizard((Wizard)scanResult[5], self, world, ref move);
                retreat = false;
            }

            if (scanResult[8]!=null)
                atackEnemyBuilding((Building)scanResult[8], self, world, ref move);

            if (scanResult[4] != null && (double)scanResult[6] < 212)  //побег от миньона
            {
                speed = 4;
                movementTo(world, self, ref move, retreatCoor, speed, false);
                retreat = true;
                if (angle < 1.57 && angle > -1.57)
                    movementTo(world, self, ref move, retreatCoor, speed, true);
                if ((double)scanResult[6] < 100)
                {
                    retreat = false;
                    atackEnemyWizard((Wizard)scanResult[4], self, world, ref move);
                }
                move.Action = ActionType.MagicMissile;
            }

            if (scanResult[0] != null && (double)scanResult[2] < 212) //побег от мага
            {
                speed = 4;
                movementTo(world, self, ref move, retreatCoor, speed, false);
                retreat = true;
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
            }

            if (scanResult[0] == null && scanResult[4] == null && !retreat)
            {
                movementTo(world, self, ref move, checkPoint, speed, true);
                if (distanceToCheckPoint < 50 && !retreat)
                {
                    move.Speed = 0;
                    stage++;
                }
                if (distanceToCheckPoint < 50 && retreat)
                {
                    move.Speed = 0;
                    stage--;
                }
            }

            if (self.Life - prevLife > 50)
            {
                stage = 0;
                scanResult[0] = null;
                scanResult[4] = null;
                retreat = false;
            }
            prevLife = self.Life;
        }

        public void Move(Wizard self, World world, Game game, Move move)
        {
            double distanceToCheckPoint;
            double distanceToRetreatPoint;
            double speed = 0, angleToRetreat= self.GetAngleTo(retreatCoor[0], retreatCoor[1]);
            //Console.WriteLine($"{self.X} {self.Y} {stage} {self.Life} {self.Life-prevLife}");

            switch (stage) // хождение по чекпоинтам
            {
                case 0:
                    checkPoint[0] = 250;
                    checkPoint[1] = 2700;
                    retreatCoor[0] = 420;
                    retreatCoor[1] = 3900;
                    speed = 2.5;
                    break;
                case 1:
                    checkPoint[0] = 500;
                    checkPoint[1] = 200;
                    retreatCoor[0] = 420;
                    retreatCoor[1] = 3900;
                    speed = 3.3;
                    break;
                case 2:
                    checkPoint[0] = 800;
                    checkPoint[1] = 100;
                    retreatCoor[0] = 420;
                    retreatCoor[1] = 2000;
                    speed = 2.6;
                    break;
                case 3:
                    checkPoint[0] = 2000;
                    checkPoint[1] = 300;
                    retreatCoor[0] = 500;
                    retreatCoor[1] = 300;
                    speed = 2.7;
                    break;
                case 4:
                    checkPoint[0] = 3800;
                    checkPoint[1] = 300;
                    retreatCoor[0] = 500;
                    retreatCoor[1] = 300;
                    speed = 2.7;
                    break;
                default:
                    break;
            }
            distanceToCheckPoint = self.GetDistanceTo(checkPoint[0], checkPoint[1]) - self.Radius;
            distanceToRetreatPoint = self.GetDistanceTo(retreatCoor[0], retreatCoor[1]) - self.Radius;
            movementControl(ref move,self,world,ref speed,distanceToCheckPoint);
        }
    }
}
