using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostgresDiff
{

    public class log_ddl_changes : ISQLQuery
    {
        public string sqlquerytext { get { return sqlq; } }
        public static string sqlq = @"---DROP TABLE IF EXISTS ddl_posgresqllog ;

---drop sequence if exists ddl_posgresqllog_id_seq;
-- Sequence oluşturuluyor (Tablo henüz olmadığı için OWNED BY kullanmıyoruz)
CREATE SEQUENCE IF NOT EXISTS ddl_posgresqllog_id_seq;

-- Yeni tablo oluşturuluyor
CREATE table if not exists ddl_posgresqllog (
    id INTEGER PRIMARY KEY DEFAULT nextval('ddl_posgresqllog_id_seq'),
    logtime TIMESTAMP,
    object_schema TEXT,
    object_type text,
    object_name TEXT,
    logcommand TEXT
);

-- Sequence'i tabloya bağlıyoruz
ALTER sequence  ddl_posgresqllog_id_seq OWNED BY ddl_posgresqllog.id;

-- Index oluşturuluyor
CREATE index if not exists idx_object_schema_name ON ddl_posgresqllog (object_schema, object_name);


---select * from ddl_posgresqllog

---drop function search_log_entries;


CREATE OR REPLACE FUNCTION log_ddl_changes()
RETURNS event_trigger AS $$
DECLARE 
    rec RECORD;
    generatedcommandrec record;
funcviewtablemastercache2 record;
reallogcommandrec record;
shortobjectidentity text ; 
    table_exists BOOLEAN;
  recexists int4;
iteration int4;
offvalue int4;
BEGIN
  ---
     --- return ;
    -- ddl_log tablosunun var olup olmadığını kontrol et
    SELECT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_name = 'ddl_log'
    ) INTO table_exists;
     ----
    -- Eğer tablo yoksa oluştur
    IF NOT table_exists THEN
        CREATE TABLE ddl_log (
            LogTime       timestamp,
            classid       oid,       -- Sistem kataloğunun OID'si
            objid         oid,       -- İşlem yapılan nesnenin OID’si
            objsubid      integer,   -- Alt nesne kimliği (genellikle 0)
            command_tag   text,      -- DDL komut etiketi (örn. CREATE TABLE, ALTER TABLE)
            object_type   text,      -- Nesne türü (örn. table, index, function)
            schema_name   text,      -- Şema adı
            object_identity text,    -- Nesne adı
            in_extension  boolean,   -- Uzantının parçası mı?
            command       text       -- Gerçek SQL komutu (Metne çevrilmiş)
        );
    END IF;
    iteration = 0;
  -- DDL komutlarını döngü ile kaydet
    FOR rec IN SELECT * FROM pg_event_trigger_ddl_commands()
   LOOP
if rec.schema_name = 'pg_temp' then 
         --  raise notice 'atladim % ', rec.schema_name ;
           continue;
    end if;
--
     generatedcommandrec  =  public.funcviewgonder2(    rec.schema_name,   rec.object_type,  rec.objid ); 
    shortobjectidentity := (REPLACE( (rec.object_identity::text), ((rec.schema_name ||'.'))::text , ''));
    iteration = iteration + 1;
      if iteration = 1 then
    begin  
         if shortobjectidentity is null then
         raise notice 'REPLACE( %::text, %.)::text , '''');', rec.object_identity, rec.schema_name;
          end if;
   drop table if exists reallogcommandtable;
   create temp table reallogcommandtable as 
  ( select * from search_log_entries( DATE_TRUNC('milliseconds', now() AT TIME ZONE current_setting('log_timezone')),
	          shortobjectidentity));
   --
 EXCEPTION
      WHEN others THEN 
       raise notice '1111shortobjectidentity % , %--', rec.object_identity, SQLERRM;
      raise notice  '11111select * from search_log_entries( %,
	          %)' , now() AT TIME ZONE current_setting('log_timezone'), shortobjectidentity;
        raise notice '111select  REPLACE( %::text, (% )::text  , '');', rec.object_identity, rec.schema_name||'.';
      end ;
      end if;
    ---
        INSERT INTO ddl_log (LogTime,classid, objid, objsubid, command_tag, object_type, schema_name, object_identity, in_extension, command)
        VALUES (
          now() AT TIME ZONE current_setting('log_timezone'),
            rec.classid, 
            rec.objid, 
            rec.objsubid,  
            rec.command_tag, 
            rec.object_type, 
            rec.schema_name, 
            rec.object_identity, 
            rec.in_extension, 
            generatedcommandrec.sqltext -- `pg_ddl_command` verisini text olarak al
        );
  ----
     recexists =( select count(*) from funcviewtablemastercache 
                where objectadi =  rec.object_identity 
                and alttip = UPPER(rec.object_type))  ;
   if recexists = 1 then 
    update  funcviewtablemastercache 
       set sqltext = generatedcommandrec.sqltext ,
        rsqltext = generatedcommandrec.rsqltext
          where objectadi =  rec.object_identity 
     and alttip = UPPER(rec.object_type)  ;
  end if;
---
 if recexists = 0 then 
    insert into  funcviewtablemastercache (alttip,  objectadi, sqltext, rsqltext)
     values ( UPPER(rec.object_type) , rec.object_identity ,
          generatedcommandrec.sqltext ,
          generatedcommandrec.rsqltext
         );
  end if;
----
  begin 
    ---
   -- select * from search_log_entries( '2025-03-24 19:42:28.742','log_ddl_changes()')
	--    select *  from reallogcommandtable  offset (1 - 1) limit 1     
---    
    begin
    offvalue = iteration - 1;
   select * into reallogcommandrec from reallogcommandtable offset offvalue limit 1 ;
       -- select * from reallogcommandtable
		if reallogcommandrec is null then
--          for reallogcommandrec in (select *  from reallogcommandtable)
--        loop
--        raise notice 'reallogcommandrec-- %',reallogcommandrec;
--         end loop;
        raise notice 'select * from search_log_entries(% ,% )',
	          now() AT TIME ZONE current_setting('log_timezone'),  shortobjectidentity;
		    raise notice 'Bulamadım % ', iteration;
           else
    begin
insert into ddl_posgresqllog (logtime ,
	    object_schema ,
        object_type,
	    object_name ,
	    logcommand ) 
	    values (  now() AT TIME ZONE current_setting('log_timezone'), 
              rec.schema_name,
                  rec.object_type,   
              shortobjectidentity, 
                 reallogcommandrec.log_block);
--
  EXCEPTION
	    WHEN others THEN 
	  --
       raise notice  'select * from search_log_entries( % ,  )); -- %' ,
       now() AT TIME ZONE current_setting('log_timezone'), SQLERRM
	          ;   
----
   end ;
	end if;
     ---   
  EXCEPTION
	    WHEN others THEN 
        raise notice ' % dongu  HAAAAAAAAAAAAAAAAAAATAAAAA %' , iteration, SQLERRM;
--
   end ;
------Buraya logun son kaydında gercek query alınacak mamiiiiiiiiiiiii
	      -----
	      EXCEPTION
	    WHEN others THEN 
	  
       raise notice  'SONNNNNNN %', SQLERRM
	          ;   
----
   end ;
-----
    END LOOP;
    EXCEPTION
    WHEN others THEN 
    raise notice 'log hatası222222 : %', SQLERRM;
raise notice  'select * from search_log_entries( % , % ));' ,
       now() AT TIME ZONE current_setting('log_timezone'),
	          shortobjectidentity;   
END;
$$ LANGUAGE plpgsql;

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_event_trigger WHERE evtname = 'track_ddl_changes'
    ) THEN
        CREATE EVENT TRIGGER track_ddl_changes
        ON ddl_command_end
        EXECUTE FUNCTION log_ddl_changes();
    END IF;
END $$;

";
    }
}
