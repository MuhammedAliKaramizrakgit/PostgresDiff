using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostgresDiff
{

    public class GetTableDef : ISQLQuery
    {
        public string sqlquerytext { get { return sqlq; } }
        public static string sqlq = @"CREATE OR REPLACE FUNCTION public.pg_get_tabledef(in_schema character varying, _verbose boolean, VARIADIC arr tabledefs[] DEFAULT '{}'::tabledefs[] )
 RETURNS TABLE(in_table text, def text)
 LANGUAGE plpgsql
AS $function$
  DECLARE
    v_version        text := '2.3 December 26, 2024  GNU General Public License 3.0';
    v_schema    text := '';
    v_coldef    text := '';
    v_qualified text := '';
    v_table_ddl text;
    v_table_oid int;
    v_colrec record;
   l_rec record;
    v_constraintrec record;
    v_trigrec       record;
    v_indexrec record;
    v_rec           record;
    v_constraint_name text;
    v_constraint_def  text;
    v_pkey_def        text := '';
    v_fkey_def        text := '';
    v_fkey_defs       text := '';
    v_trigger text := '';
    v_partition_key text := '';
    v_partbound text;
    v_parent text;
    v_parent_schema text;
    v_persist text;
    v_seqname text := '';
    v_temp  text := ''; 
    v_temp2 text;
    v_relopts text;
    v_tablespace text;
    v_pgversion int;
    v_context text := '';
    bSerial boolean;
    bPartition boolean;
    bInheritance boolean;
    bRelispartition boolean;
    constraintarr text[] := '{}';
    constraintelement text;
    bSkip boolean;
	  bVerbose boolean := False;
	  v_cnt1   integer;
	  v_cnt2   integer;
	  search_path_old text := '';
	  search_path_new text := '';
	  v_partial    boolean;
	  v_pos        integer;
	  v_partinfo   text := '';
	  v_oid        oid;
	  v_partkeydef text := '';
	  v_owner      text := '';
	  v_acl        text := '';

    -- assume defaults for ENUMs at the getgo	
  	pkcnt            int := 0;
  	fkcnt            int := 0;
	  trigcnt          int := 0;
	  cmtcnt           int := 0;
	  showpartscnt     int := 0;
	  aclownercnt      int := 0;
	  acldclcnt        int := 0;
	  aclpolicycnt     int := 0;
    pktype           public.tabledefs := 'PKEY_INTERNAL';
    fktype           public.tabledefs := 'FKEYS_INTERNAL';
    trigtype         public.tabledefs := 'NO_TRIGGERS';
    arglen           integer;
  	vargs            text;
	  avarg            public.tabledefs;

    -- exception variables
    v_ret            text;
    v_diag1          text;
    v_diag2          text;
    v_diag3          text;
    v_diag4          text;
    v_diag5          text;
    v_diag6          text;
	
  BEGIN
    SET client_min_messages = 'notice';
    IF _verbose THEN bVerbose = True; END IF;
    
    SELECT setting from pg_settings where name = 'server_version_num' INTO v_pgversion;
    IF bVerbose THEN RAISE NOTICE 'pg_get_tabledef() version=%    PG version=%', v_version, v_pgversion; END IF;
    
    -- v17 fix: handle case-sensitive  
    -- v_qualified = in_schema || '.' || in_table;
	
    arglen := array_length($3, 1);
    IF arglen IS NULL THEN
        -- nothing to do, so assume defaults
        NULL;
    ELSE
        -- loop thru args
        -- IF 'NO_TRIGGERS' = ANY ($3)
        -- select array_to_string($3, ',', '***') INTO vargs;
        IF bVerbose THEN RAISE NOTICE 'arguments=%', $3; END IF;
        FOREACH avarg IN ARRAY $3 LOOP
            IF bVerbose THEN RAISE NOTICE 'arg=%', avarg; END IF;
            IF avarg = 'FKEYS_INTERNAL' OR avarg = 'FKEYS_EXTERNAL' OR avarg = 'FKEYS_NONE' THEN
                fkcnt = fkcnt + 1;
                fktype = avarg;
            ELSEIF avarg = 'INCLUDE_TRIGGERS' OR avarg = 'NO_TRIGGERS' THEN
                trigcnt = trigcnt + 1;
                trigtype = avarg;
            ELSEIF avarg = 'PKEY_EXTERNAL' THEN
                pkcnt = pkcnt + 1;
                pktype = avarg;				                
            ELSEIF avarg = 'COMMENTS' THEN
                cmtcnt = cmtcnt + 1;
            -- Issue#33 check for dups
            ELSEIF avarg = 'SHOWPARTS' THEN
                showpartscnt = showpartscnt + 1;                
            -- Issue#27
            ELSEIF avarg = 'ACL_OWNER' THEN
                aclownercnt = aclownercnt + 1;                                
            -- Issue#35
            ELSEIF avarg = 'ACL_DCL' THEN
                acldclcnt = acldclcnt + 1;                                                
            ELSEIF avarg = 'ACL_POLICIES' THEN
                aclpolicycnt = aclpolicycnt + 1;                     
                
            END IF;
        END LOOP;
        IF fkcnt > 1 THEN 
  	        RAISE WARNING 'Only one foreign key option can be provided. You provided %', fkcnt;
	          RETURN next ;
        ELSEIF trigcnt > 1 THEN 
            RAISE WARNING 'Only one trigger option can be provided. You provided %', trigcnt;
            RETURN next ;
        ELSEIF pkcnt > 1 THEN 
            RAISE WARNING 'Only one pkey option can be provided. You provided %', pkcnt;
       RETURN next ;		
        ELSEIF cmtcnt > 1 THEN 
            RAISE WARNING 'Only one comments option can be provided. You provided %', cmtcnt;
        RETURN next ;		
        ELSEIF showpartscnt > 1 THEN 
            RAISE WARNING 'Only one SHOWPARTS option can be provided. You provided %', showpartscnt;
       RETURN next ;		
        -- Issue#27
        ELSEIF aclownercnt > 1 THEN 
            RAISE WARNING 'Only one ACL_OWNER option can be provided. You provided %', aclownercnt;
            RETURN next ;		
        -- Issue#35            
        ELSEIF acldclcnt > 1 THEN 
            RAISE WARNING 'Only one ACL_DCL option can be provided. You provided %', acldclcnt;
            RETURN next ;		
        ELSEIF aclpolicycnt > 1 THEN 
            RAISE WARNING 'Only one ACL_POLICIES option can be provided. You provided %', aclpolicycnt;
           RETURN next ;		
            
        END IF;		   		   
    END IF;

    -- Issue#31 - always handle case-sensitive schemas
    v_schema = quote_ident(in_schema);

for l_rec in (select 
      relname as tabadi
FROM pg_class
WHERE relnamespace = (SELECT oid FROM pg_namespace WHERE nspname = in_schema)
AND relkind = 'r'  and relname <> 'v2yedekviewfunc' -- limit 10 --and  relname like '%room%'
)
  loop
    BEGIN
    -- RAISE NOTICE 'DEBUG: schema qualified:%  before:%', v_schema, in_schema;
     in_table =  l_rec.tabadi;
      def = '' ;
    -- Issue#27 get owner info too
    SELECT c.oid, pg_catalog.pg_get_userbyid(c.relowner) INTO v_table_oid, v_owner FROM pg_catalog.pg_class c LEFT JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
    WHERE c.relkind in ('r','p') AND c.relname = in_table AND n.nspname = in_schema;

   -- set search_path = public before we do anything to force explicit schema qualification but dont forget to set it back before exiting...
    SELECT setting INTO search_path_old FROM pg_settings WHERE name = 'search_path';

    SELECT REPLACE(REPLACE(setting, '""$user""', '$user'), '$user', '""$user""') INTO search_path_old
    FROM pg_settings
    WHERE name = 'search_path';
    -- RAISE NOTICE 'DEBUG tableddl: saving old search_path: ***%***', search_path_old;
    EXECUTE 'SET search_path = ""public""';
    SELECT setting INTO search_path_new FROM pg_settings WHERE name = 'search_path';
    -- RAISE NOTICE 'DEBUG tableddl: using new search path=***%***', search_path_new;
    
    -- throw an error if table was not found
    IF (v_table_oid IS NULL) THEN
      RAISE EXCEPTION 'schema(%) table(%) does not exist %', v_schema, in_table, v_schema || '.' || in_table;
    END IF;    

    -- get user-defined tablespaces if applicable
    SELECT tablespace INTO v_temp FROM pg_tables WHERE schemaname = in_schema and tablename = in_table and tablespace IS NOT NULL;
    IF v_temp IS NULL THEN
      v_tablespace := 'TABLESPACE pg_default';
    ELSE
      v_tablespace := 'TABLESPACE ' || v_temp;
    END IF;
    
    -- also see if there are any SET commands for this table, ie, autovacuum_enabled=off, fillfactor=70
    WITH relopts AS (SELECT unnest(c.reloptions) relopts FROM pg_class c, pg_namespace n WHERE n.nspname = in_schema and n.oid = c.relnamespace and c.relname = in_table) 
    SELECT string_agg(r.relopts, ', ') as relopts INTO v_temp from relopts r;
    IF v_temp IS NULL THEN
      v_relopts := '';
    ELSE
      v_relopts := ' WITH (' || v_temp || ')';
    END IF;
    
    -- Issue#27: set owner ACL info
    IF aclownercnt = 1 OR acldclcnt = 1 THEN
        v_acl = 'ALTER TABLE IF EXISTS ' || quote_ident(in_schema) || '.' || quote_ident(in_table) || ' OWNER TO ' || v_owner || ';' || E'\n' || E'\n';
    END IF;
    
    -- Issue#35: add all other ACL info if directed
    -- only valid in PG 13 and above
    IF acldclcnt = 1 THEN
        -- do the revokes first
        Select 'REVOKE ALL ON TABLE ' || rtg.table_schema || '.' || rtg.table_name || ' FROM ' ||  string_agg(distinct rtg.grantee, ',' ORDER BY rtg.grantee) || ';' INTO v_temp 
		    FROM information_schema.role_table_grants rtg, pg_class c, pg_namespace n  WHERE n.nspname = quote_ident(in_schema) AND n.oid = c.relnamespace AND c.relkind in ('r','p') AND quote_ident(c.relname) = quote_ident(in_table)
        AND n.nspname = rtg.table_schema AND c.relname = rtg.table_name AND pg_catalog.pg_get_userbyid(c.relowner) <> rtg.grantee GROUP BY rtg.table_schema, rtg.table_name ORDER BY 1;
        IF v_temp <> '' THEN
            v_acl = v_acl || v_temp || E'\n' || E'\n';
        END IF;
        
        -- do the grants
        FOR v_rec IN
        WITH ACLs AS (SELECT rtg.grantee as arole,  
				CASE WHEN string_agg(rtg.privilege_type, ',' ORDER BY rtg.privilege_type) = 'DELETE,INSERT,REFERENCES,SELECT,TRIGGER,TRUNCATE,UPDATE' THEN 'ALL' ELSE string_agg(rtg.privilege_type, ',' ORDER BY rtg.privilege_type) END as privs 
				FROM information_schema.role_table_grants rtg, pg_class c, pg_namespace n  WHERE n.nspname = quote_ident(in_schema) AND n.oid = c.relnamespace AND c.relkind in ('r','p') AND c.relname = quote_ident(in_table)
				AND n.nspname = rtg.table_schema AND c.relname = rtg.table_name AND pg_catalog.pg_get_userbyid(c.relowner) <> rtg.grantee AND rtg.grantor <> rtg.grantee GROUP BY 1 ORDER BY 1)
        SELECT 'GRANT ' || acls.privs || ' ON TABLE ' || quote_ident(in_schema) || '.' || quote_ident(in_table) || ' TO ' || acls.arole || ';' as grants FROM ACLs
        LOOP
            v_acl = v_acl || v_rec.grants || E'\n';
        END LOOP;
    END IF;
    
    -- Issue#35: RLS/policies only started in PG version 13
    IF aclpolicycnt = 1 AND v_pgversion > 130000 THEN     
        v_acl = v_acl || E'\n';
        
        -- Enable row security if called for
        SELECT CASE WHEN p.polpermissive IS TRUE THEN 'true' ELSE 'false' END INTO v_temp 
        FROM pg_class c, pg_namespace n, pg_policy p WHERE n.nspname = quote_ident(in_schema) AND c.relkind in ('p','r') AND c.relname = quote_ident(in_table) AND c.oid = p.polrelid limit 1;
        IF v_temp = 'true' THEN
            v_acl =  v_acl || 'ALTER TABLE ' || quote_ident(in_schema) || '.' || quote_ident(in_table) || ' ENABLE ROW LEVEL SECURITY;' || E'\n';
        END IF;
        
        -- get policies if found
        -- For other cases to handle see examples in: https://www.postgresql.org/docs/current/ddl-rowsecurity.html
        FOR v_rec IN
        SELECT c.oid, n.nspname, c.relname, c.relrowsecurity, p.polname, p.polpermissive, pg_get_expr(p.polqual, p.polrelid) _using, pg_get_expr(p.polwithcheck, p.polrelid) acheck, 
        CASE WHEN p.polroles = '{0}' THEN '' ELSE pg_catalog.array_to_string(array(select rolname from pg_catalog.pg_roles where oid = any (p.polroles) order by 1),',') END polroles, p.polcmd, 
        'CREATE POLICY ' ||  p.polname || ' ON ' || n.nspname || '.' || c.relname || CASE WHEN p.polpermissive THEN ' AS PERMISSIVE ' ELSE ' ' END  || 
        CASE p.polcmd WHEN 'r' THEN 'FOR SELECT' WHEN 'a' THEN 'FOR SELECT' WHEN 'w' THEN 'FOR UPDATE' WHEN 'd' THEN 'FOR DELETE' ELSE 'FOR ALL'    END || ' TO ' || 
        CASE WHEN p.polroles = '{0}' THEN 'public' ELSE pg_catalog.array_to_string(array(select rolname from pg_catalog.pg_roles where oid = any (p.polroles) order by 1),',') END || 
        CASE WHEN pg_get_expr(p.polqual, p.polrelid) IS NOT NULL THEN ' USING (' || pg_get_expr(p.polqual, p.polrelid) || ')' ELSE '' END ||
        CASE WHEN pg_get_expr(p.polwithcheck, p.polrelid) IS NOT NULL THEN ' WITH CHECK (' || pg_get_expr(p.polwithcheck, p.polrelid) || ')' ELSE '' END || ';' as apolicy
        FROM pg_class c, pg_namespace n, pg_policy p WHERE n.nspname = quote_ident(in_schema) AND c.relkind in ('p','r') AND c.relname = quote_ident(in_table) AND c.oid = p.polrelid ORDER BY apolicy
        LOOP
           v_acl  = v_acl || v_rec.apolicy || E'\n'; 
        END LOOP;
    END IF;
    
    -- -----------------------------------------------------------------------------------
    -- Create table defs for partitions/children using inheritance or declarative methods.
    -- inheritance: pg_class.relkind = 'r'   pg_class.relispartition=false   pg_class.relpartbound is NULL
    -- declarative: pg_class.relkind = 'r'   pg_class.relispartition=true    pg_class.relpartbound is NOT NULL
    -- -----------------------------------------------------------------------------------
    v_partbound := '';
    bPartition := False;
    bInheritance := False;
    IF v_pgversion < 100000 THEN
      -- Issue#11: handle parent schema
      SELECT c2.relname parent, c2.relnamespace::regnamespace INTO v_parent, v_parent_schema from pg_class c1, pg_namespace n, pg_inherits i, pg_class c2
      WHERE n.nspname = in_schema and n.oid = c1.relnamespace and c1.relname = in_table and c1.oid = i.inhrelid and i.inhparent = c2.oid and c1.relkind = 'r';      
      IF (v_parent IS NOT NULL) THEN
        bPartition   := True;
        bInheritance := True;
      END IF;
    ELSE
      -- Issue#11: handle parent schema
      SELECT c2.relname parent, c1.relispartition, pg_get_expr(c1.relpartbound, c1.oid, true), c2.relnamespace::regnamespace INTO v_parent, bRelispartition, v_partbound, v_parent_schema from pg_class c1, pg_namespace n, pg_inherits i, pg_class c2
      WHERE n.nspname = in_schema and n.oid = c1.relnamespace and c1.relname = in_table and c1.oid = i.inhrelid and i.inhparent = c2.oid and c1.relkind = 'r';
      IF (v_parent IS NOT NULL) THEN
        bPartition   := True;
        IF bRelispartition THEN
          bInheritance := False;
        ELSE
          bInheritance := True;
        END IF;
      END IF;
    END IF;
    IF bPartition THEN
      --Issue#17 fix for case-sensitive tables
		  -- SELECT count(*) INTO v_cnt1 FROM information_schema.tables t WHERE EXISTS (SELECT REGEXP_MATCHES(s.table_name, '([A-Z]+)','g') FROM information_schema.tables s 
		  -- WHERE t.table_schema=s.table_schema AND t.table_name=s.table_name AND t.table_schema = quote_ident(in_schema) AND t.table_name = quote_ident(in_table) AND t.table_type = 'BASE TABLE');      
		  SELECT count(*) INTO v_cnt1 FROM information_schema.tables t WHERE EXISTS (SELECT REGEXP_MATCHES(s.table_name, '([A-Z]+)','g') FROM information_schema.tables s 
		  WHERE t.table_schema=s.table_schema AND t.table_name=s.table_name AND t.table_schema = in_schema AND t.table_name = in_table AND t.table_type = 'BASE TABLE');      		  
		  
      --Issue#19 put double-quotes around SQL keyword column names
      -- Issue#121: fix keyword lookup for table name not column name that does not apply here
      -- SELECT COUNT(*) INTO v_cnt2 FROM pg_get_keywords() WHERE word = v_colrec.column_name AND catcode = 'R';
      SELECT COUNT(*) INTO v_cnt2 FROM pg_get_keywords() WHERE word = in_table AND catcode = 'R';
		  
      IF bInheritance THEN
        -- inheritance-based
        IF v_cnt1 > 0 OR v_cnt2 > 0 THEN
          -- Issue#31 fix
          -- v_table_ddl := 'CREATE TABLE ' || in_schema || '.""' || in_table || '""( '|| E'\n';        
          v_table_ddl := 'CREATE TABLE ' || v_schema || '.""' || in_table || '""( '|| E'\n';        
        ELSE
          -- Issue#31 fix
          -- v_table_ddl := 'CREATE TABLE ' || in_schema || '.' || in_table || '( '|| E'\n';                
          v_table_ddl := 'CREATE TABLE ' || v_schema || '.' || in_table || '( '|| E'\n';                
        END IF;

        -- Jump to constraints section to add the check constraints
      ELSE
        -- declarative-based
        IF v_relopts <> '' THEN
          IF v_cnt1 > 0 OR v_cnt2 > 0 THEN
            -- Issue#31 fix
            -- v_table_ddl := 'CREATE TABLE ' || in_schema || '.""' || in_table || '"" PARTITION OF ' || in_schema || '.' || v_parent || ' ' || v_partbound || v_relopts || ' ' || v_tablespace || '; ' || E'\n';
            v_table_ddl := 'CREATE TABLE ' || v_schema || '.""' || in_table || '"" PARTITION OF ' || v_schema || '.' || v_parent || ' ' || v_partbound || v_relopts || ' ' || v_tablespace || '; ' || E'\n';
				  ELSE
				    -- Issue#31 fix
				    -- v_table_ddl := 'CREATE TABLE ' || in_schema || '.' || in_table || ' PARTITION OF ' || in_schema || '.' || v_parent || ' ' || v_partbound || v_relopts || ' ' || v_tablespace || '; ' || E'\n';
				    v_table_ddl := 'CREATE TABLE ' || v_schema || '.' || in_table || ' PARTITION OF ' || v_schema || '.' || v_parent || ' ' || v_partbound || v_relopts || ' ' || v_tablespace || '; ' || E'\n';
				  END IF;
        ELSE
          IF v_cnt1 > 0 OR v_cnt2 > 0 THEN
            -- Issue#31 fix
            -- v_table_ddl := 'CREATE TABLE ' || in_schema || '.""' || in_table || '"" PARTITION OF ' || in_schema || '.' || v_parent || ' ' || v_partbound || ' ' || v_tablespace || '; ' || E'\n';
            v_table_ddl := 'CREATE TABLE ' || v_schema || '.""' || in_table || '"" PARTITION OF ' || v_schema || '.' || v_parent || ' ' || v_partbound || ' ' || v_tablespace || '; ' || E'\n';
				  ELSE
				    -- Issue#31 fix
				    -- v_table_ddl := 'CREATE TABLE ' || in_schema || '.' || in_table || ' PARTITION OF ' || in_schema || '.' || v_parent || ' ' || v_partbound || ' ' || v_tablespace || '; ' || E'\n';
				    v_table_ddl := 'CREATE TABLE ' || v_schema || '.' || in_table || ' PARTITION OF ' || v_schema || '.' || v_parent || ' ' || v_partbound || ' ' || v_tablespace || '; ' || E'\n';
				  END IF;
        END IF;
        -- Jump to constraints and index section to add the check constraints and indexes and perhaps FKeys
      END IF;
    END IF;
	  IF bVerbose THEN RAISE NOTICE '(1)tabledef so far: %', v_table_ddl; END IF;

    IF NOT bPartition THEN
      -- see if this is unlogged or temporary table
      select c.relpersistence into v_persist from pg_class c, pg_namespace n where n.nspname = in_schema and n.oid = c.relnamespace and c.relname = in_table and c.relkind = 'r';
      IF v_persist = 'u' THEN
        v_temp := 'UNLOGGED';
      ELSIF v_persist = 't' THEN
        v_temp := 'TEMPORARY';
      ELSE
        v_temp := '';
      END IF;
    END IF;
    
    -- start the create definition for regular tables unless we are in progress creating an inheritance-based child table
    IF NOT bPartition THEN
      --Issue#17 fix for case-sensitive tables
      -- SELECT count(*) INTO v_cnt1 FROM information_schema.tables t WHERE EXISTS (SELECT REGEXP_MATCHES(s.table_name, '([A-Z]+)','g') FROM information_schema.tables s 
      -- WHERE t.table_schema=s.table_schema AND t.table_name=s.table_name AND t.table_schema = quote_ident(in_schema) AND t.table_name = quote_ident(in_table) AND t.table_type = 'BASE TABLE');   
      SELECT count(*) INTO v_cnt1 FROM information_schema.tables t WHERE EXISTS (SELECT REGEXP_MATCHES(s.table_name, '([A-Z]+)','g') FROM information_schema.tables s 
      WHERE t.table_schema=s.table_schema AND t.table_name=s.table_name AND t.table_schema = in_schema AND t.table_name = in_table AND t.table_type = 'BASE TABLE');         
      IF v_cnt1 > 0 THEN
        -- Issue#31 fix
        -- v_table_ddl := 'CREATE ' || v_temp || ' TABLE ' || in_schema || '.""' || in_table || '"" (' || E'\n';
        v_table_ddl := 'CREATE ' || v_temp || ' TABLE ' || v_schema || '.""' || in_table || '"" (' || E'\n';
      ELSE
        -- Issue#31 fix
        -- v_table_ddl := 'CREATE ' || v_temp || ' TABLE ' || in_schema || '.' || in_table || ' (' || E'\n';
        v_table_ddl := 'CREATE ' || v_temp || ' TABLE ' || v_schema || '.' || in_table || ' (' || E'\n';
      END IF;
    END IF;
    -- RAISE NOTICE 'DEBUG2: tabledef so far: %', v_table_ddl;    
    -- define all of the columns in the table unless we are in progress creating an inheritance-based child table
    IF NOT bPartition THEN
      FOR v_colrec IN
        SELECT c.column_name, c.data_type, c.udt_name, c.udt_schema, c.character_maximum_length, c.is_nullable, c.column_default, c.numeric_precision, c.numeric_scale, c.is_identity, c.identity_generation, c.is_generated, c.generation_expression        
        FROM information_schema.columns c WHERE (table_schema, table_name) = (in_schema, in_table) ORDER BY ordinal_position
      LOOP
         -- v17 fix: handle case-sensitive for pg_get_serial_sequence that requires SQL Identifier handling
         -- SELECT pg_get_serial_sequence(v_qualified, v_colrec.column_name) into v_temp;
         -- v17 fix: handle case-sensitive for pg_get_serial_sequence that requires SQL Identifier handling
         -- SELECT CASE WHEN pg_get_serial_sequence(v_qualified, v_colrec.column_name) IS NOT NULL THEN True ELSE False END into bSerial;
         SELECT pg_get_serial_sequence(quote_ident(in_schema) || '.' || quote_ident(in_table), v_colrec.column_name) into v_seqname;         
         IF v_seqname IS NULL THEN v_seqname = ''; END IF;
         SELECT CASE WHEN pg_get_serial_sequence(quote_ident(in_schema) || '.' || quote_ident(in_table), v_colrec.column_name) IS NOT NULL THEN True ELSE False END into bSerial;          
         
         -- Issue#36: call pg_get_coldef() differently
         IF v_pgversion < 100000 THEN
             SELECT public.pg_get_coldef(in_schema, in_table,v_colrec.column_name,true) INTO v_coldef;                  
         ELSE
             SELECT public.pg_get_coldef(in_schema, in_table,v_colrec.column_name) INTO v_coldef;         
         END IF;

         IF bVerbose THEN 
             -- RAISE NOTICE '(col loop) coldef=%  name=%  type=%  udt_name=%  default=%  is_generated=%  gen_expr=%  Serial=%  SeqName=%', 
             --                       v_coldef, v_colrec.column_name, v_colrec.data_type, v_colrec.udt_name, v_colrec.column_default, v_colrec.is_generated, v_colrec.generation_expression, bSerial, v_seqname;
             RAISE NOTICE '(col loop) coldef=%  name=%  type=%  udt_name=%  default=%  is_generated=%  gen_expr=%  Serial=%  SeqName=%', 
                                      v_coldef, v_colrec.column_name, v_colrec.data_type, quote_ident(v_colrec.udt_name), v_colrec.column_default, v_colrec.is_generated, v_colrec.generation_expression, bSerial, v_seqname;                                      
         END IF;
         
         --Issue#17 put double-quotes around case-sensitive column names
         SELECT COUNT(*) INTO v_cnt1 FROM information_schema.columns t WHERE EXISTS (SELECT REGEXP_MATCHES(s.column_name, '([A-Z]+)','g') FROM information_schema.columns s 
         WHERE t.table_schema=s.table_schema and t.table_name=s.table_name and t.column_name=s.column_name AND t.table_schema = quote_ident(in_schema) AND column_name = v_colrec.column_name);         

         --Issue#19 put double-quotes around SQL keyword column names         
         SELECT COUNT(*) INTO v_cnt2 FROM pg_get_keywords() WHERE word = v_colrec.column_name AND catcode = 'R';
          if ( SELECT regexp_match(v_colrec.column_name, '[ ,]')) is not null then v_cnt1 = v_cnt1 + 1; end if;
         IF v_cnt1 > 0 OR v_cnt2 > 0 THEN
           v_table_ddl := v_table_ddl || '  ""' || v_colrec.column_name || '"" ';
         ELSE
           v_table_ddl := v_table_ddl || '  ' || v_colrec.column_name || ' ';
         END IF;
         
         IF v_colrec.column_default ILIKE 'nextval%' THEN
             -- Issue#32: handle explicit sequences for serial types as well simulating pg_dump manner.
             v_temp = v_colrec.data_type || ' NOT NULL DEFAULT ' || v_colrec.column_default;
         
         ELSEIF v_colrec.is_generated = 'ALWAYS' and v_colrec.generation_expression IS NOT NULL THEN
             -- Issue#23: Handle autogenerated columns and rewrite as a simpler IF THEN ELSE branch instead of a much more complex embedded CASE STATEMENT
             -- searchable tsvector GENERATED ALWAYS AS (to_tsvector('simple'::regconfig, COALESCE(translate(email, '@.-'::citext, ' '::text), ''::text)) ) STORED
             v_temp = v_colrec.data_type || ' GENERATED ALWAYS AS (' || v_colrec.generation_expression || ') STORED ';
             
         ELSEIF v_colrec.udt_name in ('geometry') THEN
             --Issue#30 fix handle geometries separately and use coldef func on it
             -- Issue#36: call pg_get_coldef() differently
						 IF v_pgversion < 100000 THEN
						     v_temp = public.pg_get_coldef(in_schema, in_table,v_colrec.column_name, true);
						 ELSE
						     v_temp = public.pg_get_coldef(in_schema, in_table,v_colrec.column_name);
						 END IF;

         ELSEIF v_colrec.udt_name in ('box2d', 'box2df', 'box3d', 'geography', 'geometry_dump', 'gidx', 'spheroid', 'valid_detail') THEN         
		         v_temp = v_colrec.udt_name;

		     ELSEIF v_colrec.data_type = 'USER-DEFINED' THEN
		         -- Issue#31 fix
		         -- v_temp = v_colrec.udt_schema || '.' || v_colrec.udt_name;
		         -- Issue#37 handle case-sensitive user-defined types
		         -- v_temp = quote_ident(v_colrec.udt_schema) || '.' || v_colrec.udt_name;
		         v_temp = quote_ident(v_colrec.udt_schema) || '.' || quote_ident(v_colrec.udt_name);

		     ELSEIF v_colrec.data_type = 'ARRAY' THEN
   		       -- Issue#6 fix: handle arrays
             
             -- Issue#36: call pg_get_coldef() differently
						 IF v_pgversion < 100000 THEN
						     v_temp = public.pg_get_coldef(in_schema, in_table,v_colrec.column_name, true);
						 ELSE
						     v_temp = public.pg_get_coldef(in_schema, in_table,v_colrec.column_name);
						 END IF;
   		       
             -- v17 fix: handle case-sensitive for pg_get_serial_sequence that requires SQL Identifier handling
  		       -- WHEN pg_get_serial_sequence(v_qualified, v_colrec.column_name) IS NOT NULL 

		     ELSEIF pg_get_serial_sequence(quote_ident(in_schema) || '.' || quote_ident(in_table), v_colrec.column_name) IS NOT NULL THEN
		         -- Issue#8 fix: handle serial. Note: NOT NULL is implied so no need to declare it explicitly

             -- Issue#36: call pg_get_coldef() differently
						 IF v_pgversion < 100000 THEN
						     v_temp = public.pg_get_coldef(in_schema, in_table,v_colrec.column_name, true);
						 ELSE
						     v_temp = public.pg_get_coldef(in_schema, in_table,v_colrec.column_name);
						 END IF;
		         
         --ELSEIF (v_colrec.data_type = 'character varying' or v_colrec.udt_name = 'varchar') AND v_colrec.character_maximum_length IS NOT NULL THEN
		     ELSE
		         -- Issue#31 fix
		         -- v_temp = v_colrec.data_type;
		         v_temp = v_coldef;
         END IF;

         -- handle IDENTITY columns
		     IF v_colrec.is_identity = 'YES' THEN
		         IF v_colrec.identity_generation = 'ALWAYS' THEN 
		             v_temp = v_temp || ' GENERATED ALWAYS AS IDENTITY NOT NULL';
		         ELSE
		             v_temp = v_temp || ' GENERATED BY DEFAULT AS IDENTITY NOT NULL';
		         END IF;
         -- Issue#31: no need to add stuff since we get the coldef definition now above		         
         -- ELSEIF v_colrec.character_maximum_length IS NOT NULL THEN 
         --     v_temp = v_temp || ('(' || v_colrec.character_maximum_length || ')');
         -- ELSEIF v_colrec.numeric_precision > 0 AND v_colrec.numeric_scale > 0 THEN 
         --     v_temp = v_temp || '(' || v_colrec.numeric_precision || ',' || v_colrec.numeric_scale || ')';
         END IF;
         
         -- Handle NULL/NOT NULL
         IF POSITION('NOT NULL ' IN v_temp) > 0 THEN
             -- Issue#32: for explicit sequences with nextval, we already handled NOT NULL, so ignore    
             NULL;
         
         ELSEIF bSerial AND v_colrec.is_identity = 'NO' THEN 
             -- Issue#28 - added identity check 
             v_temp = v_temp || ' NOT NULL';
         
         ELSEIF v_colrec.is_nullable = 'NO' AND v_colrec.is_identity = 'NO' THEN 
             -- Issue#28 - added identity check              
             v_temp = v_temp || ' NOT NULL';

         ELSEIF v_colrec.is_nullable = 'YES' THEN
             v_temp = v_temp || ' NULL';
         END IF;

         -- Handle defaults
         -- Issue#32 fix
          -- IF v_colrec.column_default IS NOT null AND NOT bSerial THEN 
         IF v_colrec.column_default IS NOT null AND NOT bSerial AND v_colrec.column_default NOT ILIKE 'nextval%' THEN          
             -- RAISE NOTICE 'Setting default for column, %', v_colrec.column_name;
             v_temp = v_temp || (' DEFAULT ' || v_colrec.column_default);
         END IF;
         
         v_temp = v_temp || ',' || E'\n';
         -- RAISE NOTICE 'column def2=%', v_temp;
         v_table_ddl := v_table_ddl || v_temp;
         -- RAISE NOTICE 'tabledef=%', v_table_ddl;
         
         IF bVerbose THEN RAISE NOTICE 'tabledef: %', v_table_ddl; END IF;
      END LOOP;
    END IF;
    IF bVerbose THEN RAISE NOTICE '(2)tabledef so far: %', v_table_ddl; END IF;
        
    -- define all the constraints: conparentid does not exist pre PGv11
    IF v_pgversion < 110000 THEN
      FOR v_constraintrec IN
        SELECT con.conname as constraint_name, con.contype as constraint_type,
          CASE
            WHEN con.contype = 'p' THEN 1 -- primary key constraint
            WHEN con.contype = 'u' THEN 2 -- unique constraint
            WHEN con.contype = 'f' THEN 3 -- foreign key constraint
            WHEN con.contype = 'c' THEN 4
            ELSE 5
          END as type_rank,
          pg_get_constraintdef(con.oid) as constraint_definition
        FROM pg_catalog.pg_constraint con JOIN pg_catalog.pg_class rel ON rel.oid = con.conrelid JOIN pg_catalog.pg_namespace nsp ON nsp.oid = connamespace
        WHERE nsp.nspname = in_schema AND rel.relname = in_table ORDER BY type_rank
      LOOP
        v_constraint_name := v_constraintrec.constraint_name;
        v_constraint_def  := v_constraintrec.constraint_definition;
      
        IF v_constraintrec.type_rank = 1 THEN
            IF pkcnt = 0 OR pktype = 'PKEY_INTERNAL' THEN
                -- internal def
                v_constraint_name := v_constraintrec.constraint_name;
                v_constraint_def  := v_constraintrec.constraint_definition;
                v_table_ddl := v_table_ddl || '  ' -- note: two char spacer to start, to indent the column
                  || 'CONSTRAINT' || ' '
                  || v_constraint_name || ' '
                  || v_constraint_def
                  || ',' || E'\n';
            ELSE
              -- Issue#16 handle external PG def
              SELECT 'ALTER TABLE ONLY ' || in_schema || '.' || c.relname || ' ADD CONSTRAINT ' || r.conname || ' ' || pg_catalog.pg_get_constraintdef(r.oid, true) || ';' INTO v_pkey_def 
              FROM pg_catalog.pg_constraint r, pg_class c, pg_namespace n where r.conrelid = c.oid and  r.contype = 'p' and n.oid = r.connamespace and n.nspname = in_schema AND c.relname = in_table and r.conname = v_constraint_name;             
            END IF;
            IF bPartition THEN
              continue;
            END IF;
        ELSIF v_constraintrec.type_rank = 3 THEN
            -- handle foreign key constraints
            --Issue#22 fix: added FKEY_NONE check
            IF fktype = 'FKEYS_NONE' THEN
                -- skip
                continue;
            ELSIF fkcnt = 0 OR fktype = 'FKEYS_INTERNAL' THEN
                -- internal def
                v_table_ddl := v_table_ddl || '  ' -- note: two char spacer to start, to indent the column
                  || 'CONSTRAINT' || ' '
                  || v_constraint_name || ' '
                  || v_constraint_def
                  || ',' || E'\n';                
            ELSE
                -- external def
                SELECT 'ALTER TABLE ONLY ' || n.nspname || '.' || c2.relname || ' ADD CONSTRAINT ' || r.conname || ' ' || pg_catalog.pg_get_constraintdef(r.oid, true) || ';' INTO v_fkey_def 
  			        FROM pg_constraint r, pg_class c1, pg_namespace n, pg_class c2 where r.conrelid = c1.oid and  r.contype = 'f' and n.nspname = in_schema and n.oid = r.connamespace and r.conrelid = c2.oid and c2.relname = in_table;
                v_fkey_defs = v_fkey_defs || v_fkey_def || E'\n';
            END IF;
        ELSE
            -- handle all other constraints besides PKEY and FKEYS as internal defs by default
            v_table_ddl := v_table_ddl || '  ' -- note: two char spacer to start, to indent the column
              || 'CONSTRAINT' || ' '
              || v_constraint_name || ' '
              || v_constraint_def
              || ',' || E'\n';            
        END IF;
        if bVerbose THEN RAISE NOTICE 'constraint name=% constraint_def=%', v_constraint_name,v_constraint_def; END IF;
        constraintarr := constraintarr || v_constraintrec.constraint_name:: text;
  
      END LOOP;
    ELSE
      -- handle PG versions 11 and up
      -- Issue#20: Fix logic for external PKEY and FKEYS
      FOR v_constraintrec IN
        SELECT quote_ident(con.conname) as constraint_name, con.contype as constraint_type,
          CASE
            WHEN con.contype = 'p' THEN 1 -- primary key constraint
            WHEN con.contype = 'u' THEN 2 -- unique constraint
            WHEN con.contype = 'f' THEN 3 -- foreign key constraint
            WHEN con.contype = 'c' THEN 4
            ELSE 5
          END as type_rank,
          pg_get_constraintdef(con.oid) as constraint_definition
        FROM pg_catalog.pg_constraint con JOIN pg_catalog.pg_class rel ON rel.oid = con.conrelid JOIN pg_catalog.pg_namespace nsp ON nsp.oid = connamespace
        WHERE nsp.nspname = in_schema AND rel.relname = in_table 
              --Issue#13 added this condition:
              AND con.conparentid = 0 
              ORDER BY type_rank
      LOOP
        v_constraint_name := v_constraintrec.constraint_name;
        v_constraint_def  := v_constraintrec.constraint_definition;
      -- raise notice 'MAMi %', v_constraintrec.constraint_name;
      

      
        IF v_constraintrec.type_rank = 1 THEN
            IF pkcnt = 0 OR pktype = 'PKEY_INTERNAL' THEN
                -- internal def
                v_constraint_name := v_constraintrec.constraint_name;
                v_constraint_def  := v_constraintrec.constraint_definition;
                v_table_ddl := v_table_ddl || '  ' -- note: two char spacer to start, to indent the column
                  || 'CONSTRAINT' || ' '
                  || v_constraint_name || ' '
                  || v_constraint_def
                  || ',' || E'\n';
            ELSE
              -- Issue#16 handle external PG def
              SELECT 'ALTER TABLE ONLY ' || in_schema || '.' || c.relname || ' ADD CONSTRAINT ' || r.conname || ' ' || pg_catalog.pg_get_constraintdef(r.oid, true) || ';' INTO v_pkey_def 
              FROM pg_catalog.pg_constraint r, pg_class c, pg_namespace n where r.conrelid = c.oid and  r.contype = 'p' and n.oid = r.connamespace and n.nspname = in_schema AND c.relname = in_table;              
            END IF;
            IF bPartition THEN
              continue;
            END IF;
        ELSIF v_constraintrec.type_rank = 3 THEN
            -- handle foreign key constraints
            --Issue#22 fix: added FKEY_NONE check
            IF fktype = 'FKEYS_NONE' THEN
                -- skip
                continue;            
            ELSIF fkcnt = 0 OR fktype = 'FKEYS_INTERNAL' THEN
                -- internal def
                v_table_ddl := v_table_ddl || '  ' -- note: two char spacer to start, to indent the column
                  || 'CONSTRAINT' || ' '
                  || v_constraint_name || ' '
                  || v_constraint_def
                  || ',' || E'\n';                
            ELSE
                -- external def
                SELECT 'ALTER TABLE ONLY ' || n.nspname || '.' || c2.relname || ' ADD CONSTRAINT ' || r.conname || ' ' || pg_catalog.pg_get_constraintdef(r.oid, true) || ';' INTO v_fkey_def 
  			        FROM pg_constraint r, pg_class c1, pg_namespace n, pg_class c2 where r.conrelid = c1.oid and  r.contype = 'f' and n.nspname = in_schema and n.oid = r.connamespace and r.conrelid = c2.oid and c2.relname = in_table and 
  			        r.conname = v_constraint_name and r.conparentid = 0;
                v_fkey_defs = v_fkey_defs || v_fkey_def || E'\n';
            END IF;
        ELSE
            -- handle all other constraints besides PKEY and FKEYS as internal defs by default
            v_table_ddl := v_table_ddl || '  ' -- note: two char spacer to start, to indent the column
              || 'CONSTRAINT' || ' '
              || v_constraint_name || ' '
              || v_constraint_def
              || ',' || E'\n';            
        END IF;
        if bVerbose THEN RAISE NOTICE 'constraint name=% constraint_def=%', v_constraint_name,v_constraint_def; END IF;
        constraintarr := constraintarr || v_constraintrec.constraint_name:: text;
  
       END LOOP;
    END IF;      
	
    -- drop the last comma before ending the create statement, which should be right before the carriage return character
    -- Issue#24: make sure the comma is there before removing it
    select substring(v_table_ddl, length(v_table_ddl) - 1, 1) INTO v_temp;
    IF v_temp = ',' THEN
        v_table_ddl = substr(v_table_ddl, 0, length(v_table_ddl) - 1) || E'\n';
    END IF;
    IF bVerbose THEN RAISE NOTICE '(3)tabledef so far: %', trim(v_table_ddl); END IF;

    -- ---------------------------------------------------------------------------
    -- at this point we have everything up to the last table-enclosing parenthesis
    -- ---------------------------------------------------------------------------
    IF bVerbose THEN RAISE NOTICE '(4)tabledef so far: %', v_table_ddl; END IF;

    -- See if this is an inheritance-based child table and finish up the table create.
    IF bPartition and bInheritance THEN
      -- Issue#11: handle parent schema
      -- v_table_ddl := v_table_ddl || ') INHERITS (' || in_schema || '.' || v_parent || ') ' || E'\n' || v_relopts || ' ' || v_tablespace || ';' || E'\n';
      IF v_parent_schema = '' OR v_parent_schema IS NULL THEN v_parent_schema = in_schema; END IF;
      v_table_ddl := v_table_ddl || ') INHERITS (' || v_parent_schema || '.' || v_parent || ') ' || E'\n' || v_relopts || ' ' || v_tablespace || ';' || E'\n';
    END IF;

    IF v_pgversion >= 100000 AND NOT bPartition and NOT bInheritance THEN
      -- See if this is a partitioned table (pg_class.relkind = 'p') and add the partitioned key 
      SELECT pg_get_partkeydef(c1.oid) as partition_key INTO v_partition_key FROM pg_class c1 JOIN pg_namespace n ON (n.oid = c1.relnamespace) LEFT JOIN pg_partitioned_table p ON (c1.oid = p.partrelid) 
      WHERE n.nspname = in_schema and n.oid = c1.relnamespace and c1.relname = in_table and c1.relkind = 'p';

      IF v_partition_key IS NOT NULL AND v_partition_key <> '' THEN
        -- add partition clause
        -- NOTE:  cannot specify default tablespace for partitioned relations
        -- v_table_ddl := v_table_ddl || ') PARTITION BY ' || v_partition_key || ' ' || v_tablespace || ';' || E'\n';  
        v_table_ddl := v_table_ddl || ') PARTITION BY ' || v_partition_key || ';' || E'\n';  
      ELSEIF v_relopts <> '' THEN
        v_table_ddl := v_table_ddl || ') ' || v_relopts || ' ' || v_tablespace || ';' || E'\n';  
      ELSE
        -- end the create definition
        v_table_ddl := v_table_ddl || ') ' || v_tablespace || ';' || E'\n';    
      END IF;  
    END IF;

    IF bVerbose THEN RAISE NOTICE '(5)tabledef so far: %', v_table_ddl; END IF;
    
    -- Add closing paren for regular tables
    -- IF NOT bPartition THEN
    -- v_table_ddl := v_table_ddl || ') ' || v_relopts || ' ' || v_tablespace || E';\n';  
    -- END IF;
    -- RAISE NOTICE 'ddlsofar3: %', v_table_ddl;

    -- Issue#27: add OWNER ACL OR ALL_ACLS info here if directed
    IF v_acl <> '' THEN
        v_table_ddl := v_table_ddl || v_acl || E'\n';    
    END IF;

    -- Issue#16 create the external PKEY def if indicated
    IF v_pkey_def <> '' THEN
        v_table_ddl := v_table_ddl || v_pkey_def || E'\n';    
    END IF;
   
    -- Issue#20
    IF v_fkey_defs <> '' THEN
	         v_table_ddl := v_table_ddl || v_fkey_defs || E'\n';    
    END IF;
   
    IF bVerbose THEN RAISE NOTICE '(6)tabledef so far: %', v_table_ddl; END IF;
   
    -- create indexes
    FOR v_indexrec IN
      SELECT indexdef, COALESCE(tablespace, 'pg_default') as tablespace, indexname FROM pg_indexes WHERE (schemaname, tablename) = (in_schema, in_table)
    LOOP
      -- RAISE NOTICE 'DEBUG6: indexname=%  indexdef=%', v_indexrec.indexname, v_indexrec.indexdef;             
      -- loop through constraints and skip ones already defined
      bSkip = False;
      FOREACH constraintelement IN ARRAY constraintarr
      LOOP 
         IF constraintelement = v_indexrec.indexname THEN
             -- RAISE NOTICE 'DEBUG7: skipping index, %', v_indexrec.indexname;
             -- bSkip = True; mami
             --EXIT; mami
         END IF;
      END LOOP;   
      if bSkip THEN CONTINUE; END IF;
      
      -- Add IF NOT EXISTS clause so partition index additions will not be created if declarative partition in effect and index already created on parent
      v_indexrec.indexdef := REPLACE(v_indexrec.indexdef, 'CREATE INDEX', 'CREATE INDEX IF NOT EXISTS');
      -- Fix Issue#26: do it for unique/primary key indexes as well
      v_indexrec.indexdef := REPLACE(v_indexrec.indexdef, 'CREATE UNIQUE INDEX', 'CREATE UNIQUE INDEX IF NOT EXISTS');
      -- RAISE NOTICE 'DEBUG8: adding index, %', v_indexrec.indexname;
      
      -- NOTE:  cannot specify default tablespace for partitioned relations
      IF v_partition_key IS NOT NULL AND v_partition_key <> '' THEN
          v_table_ddl := v_table_ddl || v_indexrec.indexdef || ';' || E'\n';
      ELSE
          -- Issue#25: see if partial index or not
					select CASE WHEN i.indpred IS NOT NULL THEN True ELSE False END INTO v_partial 
					FROM pg_index i JOIN pg_class c1 ON (i.indexrelid = c1.oid) JOIN pg_class c2 ON (i.indrelid = c2.oid) 
					WHERE c1.relnamespace::regnamespace::text = in_schema AND c2.relnamespace::regnamespace::text = in_schema AND c2.relname = in_table AND c1.relname = v_indexrec.indexname; 
          IF v_partial THEN
              -- Put tablespace def before WHERE CLAUSE
              v_temp = v_indexrec.indexdef;
              v_pos = POSITION(' WHERE ' IN v_temp);
              v_temp2 = SUBSTRING(v_temp, v_pos);
              v_temp  = SUBSTRING(v_temp, 1, v_pos);
              v_table_ddl := v_table_ddl || v_temp || ' TABLESPACE ' || v_indexrec.tablespace || v_temp2 || ';' || E'\n';              
          ELSE
              v_table_ddl := v_table_ddl || v_indexrec.indexdef || ' TABLESPACE ' || v_indexrec.tablespace || ';' || E'\n';
          END IF;
      END IF;
      
    END LOOP;
    IF bVerbose THEN RAISE NOTICE '(7)tabledef so far: %', v_table_ddl; END IF;

    -- Issue#20: added logic for table and column comments
    IF  cmtcnt > 0 THEN 
        FOR v_rec IN
          SELECT c.relname, 'COMMENT ON ' || CASE WHEN c.relkind in ('r','p') AND a.attname IS NULL THEN 'TABLE ' WHEN c.relkind in ('r','p') AND a.attname IS NOT NULL THEN 'COLUMN ' WHEN c.relkind = 'f' THEN 'FOREIGN TABLE ' 
                 -- Issue#140
                 -- WHEN c.relkind = 'm' THEN 'MATERIALIZED VIEW ' WHEN c.relkind = 'v' THEN 'VIEW ' WHEN c.relkind = 'i' THEN 'INDEX ' WHEN c.relkind = 'S' THEN 'SEQUENCE ' ELSE 'XX' END || n.nspname || '.' || 
                 WHEN c.relkind = 'm' THEN 'MATERIALIZED VIEW ' WHEN c.relkind = 'v' THEN 'VIEW ' WHEN c.relkind = 'i' THEN 'INDEX ' WHEN c.relkind = 'S' THEN 'SEQUENCE ' ELSE 'XX' END || quote_ident(n.nspname) || '.' ||                  
                 CASE WHEN c.relkind in ('r','p') AND a.attname IS NOT NULL THEN quote_ident(c.relname) || '.' || a.attname ELSE quote_ident(c.relname) END || ' IS '   || quote_literal(d.description) || ';' as ddl
	   	    FROM pg_class c JOIN pg_namespace n ON (n.oid = c.relnamespace) LEFT JOIN pg_description d ON (c.oid = d.objoid) LEFT JOIN pg_attribute a ON (c.oid = a.attrelid AND a.attnum > 0 and a.attnum = d.objsubid)
	   	    WHERE d.description IS NOT NULL AND n.nspname = in_schema AND c.relname = in_table ORDER BY 2 desc, ddl
        LOOP
            --RAISE NOTICE 'comments:%', v_rec.ddl;
            v_table_ddl = v_table_ddl || v_rec.ddl || E'\n';
        END LOOP;   
    END IF;
    IF bVerbose THEN RAISE NOTICE '(8)tabledef so far: %', v_table_ddl; END IF;
	
    IF trigtype = 'INCLUDE_TRIGGERS' THEN
	    -- Issue#14: handle multiple triggers for a table
      FOR v_trigrec IN
          select pg_get_triggerdef(t.oid, True) || ';' as triggerdef FROM pg_trigger t, pg_class c, pg_namespace n 
          WHERE n.nspname = in_schema and n.oid = c.relnamespace and c.relname = in_table and c.relkind = 'r' and t.tgrelid = c.oid and NOT t.tgisinternal
      LOOP
          v_table_ddl := v_table_ddl || v_trigrec.triggerdef;
          v_table_ddl := v_table_ddl || E'\n';          
          IF bVerbose THEN RAISE NOTICE 'triggerdef = %', v_trigrec.triggerdef; END IF;
      END LOOP;       	    
    END IF;
  
    IF bVerbose THEN RAISE NOTICE '(9)tabledef so far: %', v_table_ddl; END IF;
    -- add empty line
    v_table_ddl := v_table_ddl || E'\n';
    IF bVerbose THEN RAISE NOTICE '(10)tabledef so far: %', v_table_ddl; END IF;

    -- Issue#33 implementation follows
    IF showpartscnt = 1 THEN
        SELECT c.oid, pg_get_partkeydef(c.oid::pg_catalog.oid) INTO v_oid, v_partkeydef FROM pg_class c, pg_namespace n WHERE n.oid = c.relnamespace AND n.nspname = in_schema and c.relname = in_table;
        IF v_partkeydef IS NOT NULL THEN
            -- v_partinfo := 'Partition key: ' || v_partkeydef || E'\n' || 'Partitions:' || E'\n' ;
            v_partinfo := 'Partitions:' || E'\n' ;

            FOR v_rec IN
                SELECT c.oid::pg_catalog.regclass, c.relkind, inhdetachpending, pg_catalog.pg_get_expr(c.relpartbound, c.oid)
                FROM pg_catalog.pg_class c, pg_catalog.pg_inherits i WHERE c.oid = i.inhrelid AND i.inhparent = v_oid
                ORDER BY pg_catalog.pg_get_expr(c.relpartbound, c.oid) = 'DEFAULT', c.oid::pg_catalog.regclass::pg_catalog.text
            LOOP
                v_partinfo := v_partinfo || v_rec.oid || ' ' || v_rec.pg_get_expr || E'\n' ;
            END LOOP;
        END IF;
    END IF;
    IF v_partinfo <> '' THEN
        v_table_ddl = v_table_ddl || v_partinfo;
    END IF;

    -- reset search_path back to what it was
    -- Issue#29: add verbose info for searchpath stuff
    v_context = 'SEARCHPATH';
    IF search_path_old = '' THEN
      SELECT set_config('search_path', '', false) into v_temp;
      IF bVerbose THEN RAISE NOTICE 'SearchPath Cleanup: current searchpath=%', v_temp; END IF;
    ELSE
      IF bVerbose THEN RAISE NOTICE 'SearchPath Cleanup: resetting searchpath=%', search_path_old; END IF;
      EXECUTE 'SET search_path = ' || search_path_old;
    END IF;
      def = v_table_ddl;
    RETURN next ;
	
    EXCEPTION
    WHEN others THEN
    BEGIN
      GET STACKED DIAGNOSTICS v_diag1 = MESSAGE_TEXT, v_diag2 = PG_EXCEPTION_DETAIL, v_diag3 = PG_EXCEPTION_HINT, v_diag4 = RETURNED_SQLSTATE, v_diag5 = PG_CONTEXT, v_diag6 = PG_EXCEPTION_CONTEXT;
      -- v_ret := 'line=' || v_diag6 || '. '|| v_diag4 || '. ' || v_diag1 || ' .' || v_diag2 || ' .' || v_diag3;

      -- put additional coding here if necessary
      IF v_context <> '' THEN
          v_ret := 'line=' || v_diag6 || '. '|| v_diag4 || '. ' || v_diag1 || '  context=' || v_context;      
          RAISE WARNING 'Search_path not reset correctly.  You may need to adjust it manually. %', v_ret;          
      ELSE
          v_ret := 'line=' || v_diag6 || '. '|| v_diag4 || '. ' || v_diag1;
          RAISE EXCEPTION '%', v_ret;          
      END IF;
       RETURN next ;
    END;
    End;
   end  loop;
  END;
$function$
;
";

} }
