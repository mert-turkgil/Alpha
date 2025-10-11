/**
 * Cloudflare Email Worker for Alpha Safety Shoes
 * 
 * Purpose: Filter spam and verify legitimate contact form submissions
 * 
 * Features:
 * - Allows emails from verified contact form (with custom headers)
 * - Blocks high-spam external emails
 * - Quarantines medium-spam emails for review
 * - Forwards legitimate external emails
 * 
 * Deploy this to: Cloudflare Dashboard → Email Routing → Email Workers
 */

export default {
  async email(message, env, ctx) {
    
    // ========================================
    // 1. EXTRACT EMAIL METADATA
    // ========================================
    
    const from = message.from;
    const to = message.to;
    const subject = message.headers.get("subject") || "(no subject)";
    
    // Check for custom verification headers from your application
    const isFromContactForm = message.headers.get("X-Alpha-Contact-Form") === "true";
    const isTurnstileVerified = message.headers.get("X-Turnstile-Verified") === "true";
    const sentFrom = message.headers.get("X-Sent-From");
    
    // Get spam score (Cloudflare provides this automatically)
    // Score range: 0-10 (0 = definitely not spam, 10 = definitely spam)
    const spamScore = message.spamScore || 0;
    
    
    // ========================================
    // 2. LOG FOR DEBUGGING
    // ========================================
    
    console.log("==========================================");
    console.log("[Email Worker] New email received");
    console.log(`[Email Worker] From: ${from}`);
    console.log(`[Email Worker] To: ${to}`);
    console.log(`[Email Worker] Subject: ${subject}`);
    console.log(`[Email Worker] Spam Score: ${spamScore}/10`);
    console.log(`[Email Worker] From Contact Form: ${isFromContactForm}`);
    console.log(`[Email Worker] Turnstile Verified: ${isTurnstileVerified}`);
    console.log(`[Email Worker] Sent From: ${sentFrom}`);
    console.log("==========================================");
    
    
    // ========================================
    // 3. ROUTE 1: VERIFIED CONTACT FORM EMAIL
    // ========================================
    
    // If email has all verification headers, it came from your contact form
    // after passing Cloudflare Turnstile validation
    if (isFromContactForm && isTurnstileVerified && sentFrom === "Alpha-Contact-Application") {
      console.log("[Email Worker] ✅ VERIFIED contact form submission");
      console.log("[Email Worker] Action: Forward to info@alphaayakkabi.com (then auto-forwarded to personal email by Güzel Hosting)");
      
      // Forward to info@ (which auto-forwards to your personal email via Güzel Hosting)
      await message.forward("info@alphaayakkabi.com");
      return;
    }
    
    
    // ========================================
    // 4. ROUTE 2: EXTERNAL EMAILS (NOT FROM CONTACT FORM)
    // ========================================
    
    console.log("[Email Worker] ⚠️ External email detected (not from contact form)");
    console.log("[Email Worker] Applying spam filtering...");
    
    
    // ========================================
    // 4A. HIGH SPAM SCORE → REJECT
    // ========================================
    
    if (spamScore >= 7) {
      console.log(`[Email Worker] ❌ HIGH SPAM SCORE: ${spamScore}/10`);
      console.log("[Email Worker] Action: REJECT");
      
      // Reject the email completely
      message.setReject(`Spam detected with score ${spamScore}/10`);
      
      // Optional: Send notification to admin about blocked spam
      // await fetch("https://your-webhook.com/spam-blocked", {
      //   method: "POST",
      //   body: JSON.stringify({ from, to, subject, spamScore })
      // });
      
      return;
    }
    
    
    // ========================================
    // 4B. MEDIUM SPAM SCORE → QUARANTINE
    // ========================================
    
    if (spamScore >= 4) {
      console.log(`[Email Worker] ⚠️ MEDIUM SPAM SCORE: ${spamScore}/10`);
      console.log("[Email Worker] Action: Send to quarantine for review");
      
      // Forward to a separate quarantine email for manual review
      // Create this email in Cloudflare Email Routing
      await message.forward("quarantine@alphaayakkabi.com");
      
      // Optional: Also notify you on Telegram/Slack/etc
      // await fetch("https://your-notification-service.com/quarantine", {
      //   method: "POST",
      //   body: JSON.stringify({ from, to, subject, spamScore })
      // });
      
      return;
    }
    
    
    // ========================================
    // 4C. LOW SPAM SCORE → ALLOW
    // ========================================
    
    console.log(`[Email Worker] ✅ LOW SPAM SCORE: ${spamScore}/10`);
    console.log("[Email Worker] Action: Forward to info@alphaayakkabi.com");
    
    // Legitimate external email (e.g., business partners, customers replying)
    await message.forward("info@alphaayakkabi.com");
    
    
    // ========================================
    // 5. OPTIONAL: ADDITIONAL FILTERS
    // ========================================
    
    // You can add more custom filtering logic here:
    
    // Example 1: Block specific domains
    // if (from.includes("@spam-domain.com")) {
    //   message.setReject("Domain blocked");
    //   return;
    // }
    
    // Example 2: Block emails with certain keywords in subject
    // const spamKeywords = ["viagra", "casino", "lottery", "get rich"];
    // if (spamKeywords.some(keyword => subject.toLowerCase().includes(keyword))) {
    //   message.setReject("Spam keyword detected");
    //   return;
    // }
    
    // Example 3: Require DKIM/SPF pass for external emails
    // if (!message.dkim.passed || !message.spf.passed) {
    //   message.setReject("Authentication failed");
    //   return;
    // }
  }
}


/**
 * DEPLOYMENT INSTRUCTIONS:
 * 
 * 1. Go to Cloudflare Dashboard
 * 2. Select your domain: alphaayakkabi.com
 * 3. Navigate to: Email Routing → Email Workers
 * 4. Click "Create"
 * 5. Name: alpha-contact-spam-filter
 * 6. Copy/paste this entire file
 * 7. Click "Deploy"
 * 
 * 8. Configure Email Routing Rules:
 *    - support@alphaayakkabi.com → Run Worker: alpha-contact-spam-filter
 *    - info@alphaayakkabi.com → Run Worker: alpha-contact-spam-filter
 *    - quarantine@alphaayakkabi.com → Forward to: your-personal-email@gmail.com
 * 
 * 9. Test:
 *    - Submit contact form (should forward to info@)
 *    - Send test email from external source (check spam score handling)
 * 
 * MONITORING:
 * - View logs: Cloudflare Dashboard → Email Workers → Logs
 * - Check email routing logs: Email Routing → Logs
 * 
 * SPAM SCORE GUIDELINES:
 * - 0-3: Clean email (forward to info@)
 * - 4-6: Suspicious (quarantine for review)
 * - 7-10: Spam (reject completely)
 * 
 * You can adjust these thresholds based on your needs.
 */
