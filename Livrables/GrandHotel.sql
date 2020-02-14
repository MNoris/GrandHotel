--1.	Les clients pour lesquels on n’a pas de numéro de portable (id, nom)
--553 lignes
select distinct id, nom
from Client C
         join Telephone T on C.Id = T.IdClient
where id not in (select T2.IdClient from Telephone T2 where T2.CodeType = 'M')


--2.	Le taux moyen de réservation de l’hôtel par mois-année (2015-01, 2015-02…), c'est à dire la moyenne sur les chambres du ratio (nombre de jours de réservation dans le mois / nombre de jours du mois)
--47 lignes
select Annee, mois, avg(ratio)
from (select Year(Jour)                                                                                          Annee,
             Month(Jour)                                                                                         mois,
             TRY_CAST(count(distinct Jour) as DECIMAL) / Day(EOMONTH(DATEFROMPARTS(YEAR(Jour), MONTH(Jour), 1))) ratio
      from Reservation R
      group by YEAR(jour), MONTH(Jour), DATEFROMPARTS(YEAR(Jour), MONTH(Jour), 1), NumChambre) as a
group by mois, annee
order by Annee,mois


--3.	Le nombre total de jours réservés par les clients ayant une carte de fidélité au cours de la dernière année du calendrier (obtenue dynamiquement)
-- 42
select count(distinct Jour) nbJours
from Client C
         join Reservation R2 on C.Id = R2.IdClient
where CarteFidelite = 1
  and YEAR(Jour) = (select YEAR(max(C.Jour)) from Calendrier C)


--4.	Le chiffre d’affaire de l’hôtel par trimestre de chaque année
/*
ca,Annee,Trimestre
50955.245000000,2016,1
84953.000000000,2016,2
84634.000000000,2016,3
92696.340000000,2016,4
98100.332000000,2017,1
103196.720000000,2017,2
102002.120000000,2017,3
120939.500000000,2017,4
42281.800000000,2018,1
*/
select sum(MontantHT * (1 - TauxReduction) * (1 + TauxTVA) * Quantite) ca,
       YEAR(DatePaiement)                                              Annee,
       DATEPART(qq, DatePaiement)                                      Trimestre
from LigneFacture
         join Facture F on LigneFacture.IdFacture = F.Id
group by YEAR(DatePaiement), DATEPART(qq, DatePaiement)
order by YEAR(DatePaiement), DATEPART(qq, DatePaiement)


--5.	Le nombre de clients dans chaque tranche de 1000 € de chiffre d’affaire total généré. La première tranche est < 5000 €, et la dernière >= 8000 €
/*
tranche,nombre
< 5000,0
>= 8000,43
entre 5000 et 6000,1
entre 6000 et 7000,17
entre 7000 et 8000,39
 */
declare @table table
               (
                   tranche nvarchar(100) not null,
                   nombre  int           not null
               );


insert @table
select R.tranche, count(R.IdClient)
from (select IdClient,
             case
                 when sum((lf.MontantHT * (1 - lf.TauxReduction) * (1 + lf.TauxTVA) * lf.Quantite)) < 5000
                     then '< 5000'
                 when sum(
                         (lf.MontantHT * (1 - lf.TauxReduction) * (1 + lf.TauxTVA) * lf.Quantite)) between 5000 and 6000
                     then 'entre 5000 et 6000'
                 when sum(
                         (lf.MontantHT * (1 - lf.TauxReduction) * (1 + lf.TauxTVA) * lf.Quantite)) between 6000 and 7000
                     then 'entre 6000 et 7000'
                 when sum(
                         (lf.MontantHT * (1 - lf.TauxReduction) * (1 + lf.TauxTVA) * lf.Quantite)) between 7000 and 8000
                     then 'entre 7000 et 8000'
                 when sum((lf.MontantHT * (1 - lf.TauxReduction) * (1 + lf.TauxTVA) * lf.Quantite)) >= 8000
                     then '>= 8000'
                 end as tranche
      from Facture F
               join LigneFacture Lf on F.Id = Lf.IdFacture
               join Client C on C.Id = F.IdClient
      group by IdClient) as R
group by R.tranche

if not exists(select *
              from @table
              where tranche = '< 5000')
    INSERT @table
    VALUES ('< 5000', 0)
if not exists(select *
              from @table
              where tranche = 'entre 5000 et 6000')
    INSERT @table
    VALUES ('entre 5000 et 6000', 0)
if not exists(select *
              from @table
              where tranche = 'entre 6000 et 7000')
    INSERT @table
    VALUES ('entre 6000 et 7000', 0)
if not exists(select *
              from @table
              where tranche = 'entre 7000 et 8000')
    INSERT @table
    VALUES ('entre 7000 et 8000', 0)
if not exists(select *
              from @table
              where tranche = '>= 8000')
    INSERT @table
    VALUES ('>= 8000', 0)


select *
from @table
order by tranche


--6.	Code T-SQL pour augmenter à partir du 01/01/2019 les tarifs des chambres de type 1 de 5%, et ceux des chambres de type 2 de 4% par rapport à l'année précédente
/*2 rows affected
Code,DateDebut,Prix
CHB1-2019,2019-01-01,63.000
CHB2-2019,2019-01-01,80.080

20 rows affected

NumChambre,CodeTarif
1,CHB1-2019
2,CHB1-2019
3,CHB1-2019
4,CHB1-2019
5,CHB2-2019
6,CHB1-2019
7,CHB1-2019
8,CHB1-2019
9,CHB1-2019
10,CHB2-2019
11,CHB1-2019
12,CHB1-2019
14,CHB1-2019
15,CHB2-2019
16,CHB1-2019
17,CHB1-2019
18,CHB1-2019
19,CHB1-2019
20,CHB2-2019
21,CHB1-2019

 */
begin tran
    declare @prixChb1 as decimal(12, 3)= (select prix * 1.05 from Tarif where Code = 'CHB1-2018')
    declare @prixChb2 as decimal(12, 3)= (select prix * 1.04 from Tarif where Code = 'CHB2-2018')
    insert Tarif
    values ('CHB1-2019', DATEFROMPARTS(2019, 01, 01), @prixChb1),
           ('CHB2-2019', DATEFROMPARTS(2019, 01, 01), @prixChb2)
    insert TarifChambre select distinct NumChambre, substring(CodeTarif, 0, 6) + '2019' from TarifChambre
rollback


--7.	Clients qui ont passé au total au moins 7 jours à l’hôtel au cours d’un même mois (Id, Nom, mois où ils ont passé au moins 7 jours)
/*
id,nom,year,mois
341,Stimac,2015,5
505,Milionis,2017,3

 */
select id, nom, YEAR(Jour) year, MONTH(Jour) mois
from Client C
         join Reservation R on C.Id = R.IdClient
group by id, nom, YEAR(Jour), MONTH(Jour)
having count(*) >= 7

--8.	Clients qui sont restés à l’hôtel au moins deux jours de suite au cours de l’année 2017
/*
id,nom
11,PAUL
277,Aja
505,Milionis

 */
select distinct id, nom
from Client C
         join Reservation R on C.Id = R.IdClient
where year(R.Jour) = 2017
  and 1 in (select DATEDIFF(day, R.Jour, R2.Jour) from Reservation R2 where year(R2.Jour) = 2017 and R2.IdClient = id)
