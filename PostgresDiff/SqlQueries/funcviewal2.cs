using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostgresDiff
{

    public class funcviewal2 : ISQLQuery
    {
        public string sqlquerytext { get { return sqlq; } }
        public static string sqlq = @"--drop FUNCTION public.funcviewal2 ( updatemi text, _host text, _port text, _database text, she text, _user text, _password text)
-- Tuana hanım kesinlikle master a kurma

--- Tuana Type  içinde baska Type olursa haber ver mudahale ederiz mami
--- Tuana var olan Table columnları değişirse çözüm yok ama hata mesajlarından anlarsın 
CREATE OR REPLACE FUNCTION public.funcviewal2(updatemi text, _host text, _port text, _database text, she text, _user text, _password text)
RETURNS 
table  ( id3 int4 ,
       yedektarihi3 timestamp  ,
       tipi3 text ,
       altip3 text ,
    objectadi3 text ,
    clobjectadi3 text ,
       sira3 int4 ,
       sqlquery3 text ,
    reversesqlquery3 text ,
    islendi3 bool, clsqltext3 text, clrsql3 text
     
    -- gyedekid int4 ,
    --   yedektarihi timestamp  ,
    --   tipi text ,
    --   altip text ,
    --   sira int4 ,
   --- objectadi text ,
   --    sqlquery text ,
  --  reversesqlquery text
)
LANGUAGE plpgsql
AS $function$
DECLARE
temprec record;
karsi record;
karsi2 record;
karsi3 record;
karsi4 record;
a text;
f_rec record;
  l_rec record;
r_rec record;
  l_stmt text;
  donsql text = '';
  rsql text = '';
gelensql text[];
_sql text;
tektext text;
_connectionst2 text;
sonsira int4;
denemesay int4 = 0;
sonyedekid int4 = 0;
sonyedektarihi timestamp;
kayitadedi int4 = 0;
    clienttabledef text ;
bos text = '';
kayitsayisi int4;
begin
  
  l_stmt := format(
        'CREATE TABLE IF NOT EXISTS %I.v2yedekviewfunc (
            id serial4 PRIMARY KEY,
            yedekid int4 NOT NULL,
            yedektarihi timestamp NULL,
            tipi text NULL,
            altip text NULL,
            sira int4 NULL,
            objectadi text NULL,
            sqlquery text NULL,
            reversesql text NULL
        )', she);

    
    EXECUTE l_stmt;


 
 drop  table if exists v2yedekviewfunctemp;
 


create temp table if not exists v2yedekviewfunctemp (
    id serial4 NOT NULL,
       yedektarihi timestamp  NULL,
       tipi text NULL,
       altip text NULL,
    objectadi text null,
       sira int4 NULL,
       sqlquery text NULL,
    reversesqlquery text NULL,
    islendi bool,
       CONSTRAINT ""pk.v2yedekviewfunctemp"" PRIMARY KEY (id)
);
  truncate v2yedekviewfunctemp;
    

sonyedekid = coalesce((select max(yedekid) from v2yedekviewfunc),0);
  sonyedekid = sonyedekid + 1;
sonyedektarihi = now();


 
 


 

 
 
 
 
   
_connectionst2 = 'dbname=' || _database || ' port=' || _port || ' host=' || _host || ' user=' ||
_user || ' password=' || _password ; --|| ' connection_timeout = 2';
  
 
raise notice 'karsiiiiiii  -- %', _connectionst2;
    a = 'SELECT
       n.nspname || ''.'' || t.typname as sobj,
       CASE
           WHEN t.typtype = ''e'' THEN
               ''CREATE TYPE '' || n.nspname || ''.'' || t.typname || '' AS ENUM ('' ||
               pg_catalog.array_to_string (
                   ARRAY( SELECT e.enumlabel
                          FROM pg_catalog.pg_enum e
                          WHERE e.enumtypid = t.oid
                          ORDER BY e.oid ), '', ''
               ) || '');''
           WHEN t.typtype = ''c'' THEN
               -- Composite türleri için sütunları alıyoruz ve formatı düzeltip sıralama sağlıyoruz
               ''CREATE TYPE '' || n.nspname || ''.'' || t.typname || '' AS ('' ||
               (SELECT string_agg(
                   c.attname || '' '' || pg_catalog.format_type(c.atttypid, NULL),
                   '', ''
               )
               FROM pg_catalog.pg_attribute c
               WHERE c.attrelid = t.typrelid
                 AND c.attnum > 0) || '');''
           ELSE
               ''CREATE TYPE '' || n.nspname || ''.'' || t.typname || '';''
       END AS create_type_command
FROM pg_catalog.pg_type t
LEFT JOIN pg_catalog.pg_namespace n
    ON n.oid = t.typnamespace
WHERE ( t.typrelid = 0
        OR ( SELECT c.relkind = ''c''
                FROM pg_catalog.pg_class c
                WHERE c.oid = t.typrelid
            )
      )
  AND NOT EXISTS
      ( SELECT 1
          FROM pg_catalog.pg_type el
          WHERE el.oid = t.typelem
              AND el.typarray = t.oid
      )
  AND n.nspname <> ''pg_catalog''
  AND n.nspname <> ''information_schema''
  AND pg_catalog.pg_type_is_visible(t.oid)
ORDER BY 1;';

CREATE TEMP TABLE temp_types (adi TEXT, def TEXT) ON COMMIT DROP;

    -- Dinamik SQL'in çıktısını geçici tabloya ekle
    EXECUTE 'INSERT INTO temp_types ' || a;
for karsi4 in
(select  kar.def def , kar.adi adi , bu2.def budef, bu2.adi buadi from 
(select  * from
dblink(_connectionst2 ,
a

)
as _txt( adi text ,def text  ) ) kar left join (select * from  temp_types) bu2
on bu2.adi = kar.adi 
)

loop
begin 
  if karsi4.def = karsi4.budef then
    
  else
		   if karsi4.def is not null then 
		 execute karsi4.def;
		  end if;
  end if;
  exception when others then
        
       raise notice 'TYPE DEF % atlandi % ' , karsi4.def, SQLERRM;
    end;

end loop;
   a =  'SELECT
    (''CREATE SEQUENCE if not exists '' || sequencename ||
    '' START '' || COALESCE(start_value::text, ''1'') ||
    '' INCREMENT BY '' || COALESCE(increment_by::text, ''1'') ||
    '' MINVALUE '' || COALESCE(min_value::text, ''1'') ||
    '' MAXVALUE '' || COALESCE(max_value::text, ''9223372036854775807'') ||
    '' CACHE '' || COALESCE(cache_size::text, ''1'') || '';'') as def , sequencename adi
FROM pg_sequences';
CREATE TEMP TABLE temp_sequences (adi TEXT, def TEXT) ON COMMIT DROP;

    -- Dinamik SQL'in çıktısını geçici tabloya ekle
    EXECUTE 'INSERT INTO temp_sequences ' || a;

for karsi3 in
(
select  kar.adi adi, kar.def def, bu2.adi buadi, bu2.def budef from (
select * from dblink(_connectionst2 ,
a
)
as _txt(def text, adi text  )  
) kar left join (select * from  temp_sequences ) bu2 on  bu2.adi = kar.adi 
)

loop
 if  karsi4.def = karsi4.budef then
    bos = '';    --raise notice 'Squence  % var zaten  ' , karsi3.adi ;
   else
     execute karsi3.def;
 end if;
end loop;



   for temprec in
(
select  * from
dblink(_connectionst2 ,
--'select adi  from acenta limit 1'
'select * from funcviewtablemastercache'
) as _txt(alttip text, objectadi text, sqltext character varying, rsqltext character varying)
)
loop

insert into v2yedekviewfunctemp ( tipi, altip, sqlquery, islendi,reversesqlquery, objectadi)
         values ('CREATE', temprec.alttip,  temprec.sqltext, false, temprec.rsqltext, she ||'.' ||temprec.objectadi);

  -- raise notice '%' ,  karsi.alttip || karsi.objectadi; 
  --execute karsi._view;
-- continue;
end loop;
 



for temprec in (select * from funcviewtablemastercache)
    loop

 insert into v2yedekviewfunctemp ( tipi, altip, sqlquery, islendi,reversesqlquery, objectadi)
         values ('DROP', temprec.alttip,  temprec.sqltext, false, temprec.rsqltext, she ||'.' ||temprec.objectadi);
end loop;

drop table if exists v2yedekviewfunccache;

create  table if not exists  v2yedekviewfunccache as 
select * from 
(select ms.id  ,
       sonyedektarihi as yedektarihi   ,
       ms.tipi  ,
       ms.altip  ,
    ms.objectadi  ,
      cl.objectadi clobjectadi,
       1 as sira  ,
       ms.sqlquery  ,
    ms.reversesqlquery ,
    ms.islendi , cl.sqlquery clsql, cl.reversesqlquery clrsql from 
(select * from v2yedekviewfunctemp   where  tipi = 'CREATE' ) ms 
  full outer join (select * from v2yedekviewfunctemp   where  tipi = 'DROP' ) cl
  on   ms.altip = cl.altip and ms.objectadi = cl.objectadi) ;

 drop  table if exists v2yedekviewfunctemp3;

create temp table  v2yedekviewfunctemp3 as 
select id id ,
       yedektarihi yedektarihi  ,
       tipi tipi ,
       altip altip ,
    objectadi objectadi ,
    clobjectadi clobjectadi ,
       sira sira ,
       sqlquery sqlquery ,
    reversesqlquery reversesqlquery ,
    islendi islendi, clsql clsqltext, clrsql clrsql from 
( select * from v2yedekviewfunccache);

for temprec in (select * from v2yedekviewfunctemp3)
    loop

 if temprec.sqlquery = temprec.clsqltext then 
   update v2yedekviewfunctemp3 set islendi = true where id = temprec.id; 
end if;
   end loop;


drop  table if exists v2yedekviewfunctemp2;

create temp table  v2yedekviewfunctemp2 as 
select * from v2yedekviewfunctemp3 where altip = 'TABLE' ;

for temprec in (select * from v2yedekviewfunctemp2)
    loop

 if temprec.sqlquery = temprec.clsqltext then 
   update v2yedekviewfunctemp2 set islendi = true where id = temprec.id;
  else
raise notice 'geldi' ;
end if;
   end loop;



sonsira = 0;
denemesay = 0; 





while ((select  count(*) from v2yedekviewfunctemp2 t where t.islendi = false) > 0 and denemesay  < 11)
 
   loop
  denemesay = denemesay + 1;
   raise notice '%.DENEME -----------------------', denemesay;
   for temprec in
     (select * from v2yedekviewfunctemp2 t where 
      t.islendi = false)
       loop
   ----basladık işleme 
         if   temprec.altip = 'TABLE' then 
   -- table ise durum ozel eger yoksa ac 
            if temprec.clobjectadi is null then
	                begin               
	                   execute temprec.sqlquery;
	                   sonsira = sonsira + 1;
	                   update v2yedekviewfunctemp2  set islendi = true , sira =  sonsira
	                       where v2yedekviewfunctemp2.id = temprec.id ;
	                   exception when others then -- olmadi ise dependency olabilir atla 25 kere deneyecez sonuçta
	                  if denemesay = 10 then
	                          raise notice ' % karsida data yok   data olmadi ----Sql -- %', temprec.objectadi,  temprec.sqlquery;
	                         end if;
	                 end;
              else --yani burada  dosya var kayıt yoksa dropla
                  	 execute ('Select count(*) from ' || temprec.objectadi ) into kayitsayisi;
                    if kayitsayisi = 0 then 
----- veri yok dropla gitsin
                       begin   
                               
			                   execute temprec.reversesqlquery;
			                   execute temprec.sqlquery;
			                   sonsira = sonsira + 1;
			                   update v2yedekviewfunctemp2  set islendi = true , sira =  sonsira
			                       where v2yedekviewfunctemp2.id = temprec.id ;
			                   exception when others then -- olmadi ise dependency olabilir atla 25 kere deneyecez sonuçta
			                  if denemesay = 10 then
	                          raise notice ' % tablosunda  0 data olmadi ', temprec.objectadi;
	                         end if;
			              end;
                     else
	                      if denemesay = 10 then
	                          raise notice ' % tablosunda  data  droplayamam çözüm bul ', temprec.objectadi;
	                      end if;
                   end if;
               end if; 
            
        end if;
     end loop;
 end loop;
return query select * from v2yedekviewfunctemp2;




drop  table if exists v2yedekviewfunctemp5;

create temp table  v2yedekviewfunctemp5 as 
select * from v2yedekviewfunctemp3 where altip <> 'TABLE' ;

for temprec in (select * from v2yedekviewfunctemp5)
    loop

 if temprec.sqlquery = temprec.clsqltext then 
   update v2yedekviewfunctemp5 set islendi = true where id = temprec.id; 
end if;
   end loop;




denemesay = 0; 


begin 
while ((select  count(*) from v2yedekviewfunctemp5 t where t.islendi = false) > 0 and denemesay  < 11)
 
   loop
    begin
  denemesay = denemesay + 1;
     
   raise notice '%.DENEME -----------------------', denemesay;
   for temprec in
     (select * from v2yedekviewfunctemp5 t where 
      t.islendi = false)
       loop
   ----basladık işleme 
                 begin  
          
                   execute temprec.sqlquery;
                   if denemesay = 2 then
                  raise notice ' deneme --%',  temprec.sqlquery;
                  end if;
                   sonsira = sonsira + 1;
                   update v2yedekviewfunctemp5  set islendi = true , sira =  sonsira
                       where v2yedekviewfunctemp5.id = temprec.id ;
                                if denemesay = 2 then
						                  raise notice 'exception   pdate v2yedekviewfunctemp2';
						               end if;  
                   exception when others then -- olmadi ise dependency olabilir maa bir kere de droplayıp deneyelim
                   begin
						if denemesay = 2 then
						                  raise notice 'exception   deneme --%',  temprec.sqlquery;
						               end if;
                           execute temprec.reversesqlquery; 
                               if denemesay = 2 then
						                  raise notice 'geldi';
						               end if;
                          execute temprec.sqlquery;
                                      if denemesay = 2 then
						                  raise notice 'geldi  sqlquery';
						               end if;
                        sonsira = sonsira + 1;
                      update v2yedekviewfunctemp5  set islendi = true , sira =  sonsira
                       where v2yedekviewfunctemp5.id = temprec.id ;
                                  if denemesay = 2 then
						                  raise notice 'geldi  update v2yedekviewfunctemp2';
						               end if;
                       exception when others then 
		                      if denemesay = 2 then
		                          raise notice ' %   %   droplayamadim çözüm bul sql1 -- %  sql2 % % ', 
               temprec.altip , temprec.objectadi, temprec.reversesqlquery,  temprec.sqlquery, SQLERRM; 
		                      end if;
                             bos = '';              -- sonraki sefer kesin dependency
                    end;   
                  
               end ;
    
     end loop;
     exception when others then 
       raise exception 'Hataaaaa % '  , SQLERRM;
      end;
 end loop;
exception when others then 
       raise exception 'Hataaaaa % '  , SQLERRM;
end ;




end;
$function$
;
";
    }
}
